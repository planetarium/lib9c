using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Exceptions;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Nekoyume.Arena;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Summon;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("costume_summon")]
    public class CostumeSummon : GameAction
    {
        public const string AvatarAddressKey = "aa";
        public Address AvatarAddress;

        public const string GroupIdKey = "gid";
        public int GroupId;

        public const string SummonCountKey = "sc";
        public int SummonCount;

        public CostumeSummon()
        {
        }

        public CostumeSummon(Address avatarAddress, int groupId, int summonCount)
        {
            AvatarAddress = avatarAddress;
            GroupId = groupId;
            SummonCount = summonCount;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [AvatarAddressKey] = AvatarAddress.Serialize(),
                [GroupIdKey] = (Integer)GroupId,
                [SummonCountKey] = (Integer)SummonCount,
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
            GroupId = (Integer)plainValue[GroupIdKey];
            SummonCount = (Integer)plainValue[SummonCountKey];
        }

        public static IEnumerable<Costume> SimulateSummon(
            string addressesHex,
            CostumeItemSheet costumeItemSheet,
            SummonSheet.Row summonRow,
            int summonCount,
            IRandom random
        )
        {
            summonCount = SummonHelper.CalculateSummonCount(summonCount);

            var result = new List<Costume>();
            List<int> recipeIds;

            if (summonRow.UseGradeGuarantee)
            {
                // Use grade guarantee system with settings from SummonSheet.Row based on summon count
                recipeIds = SummonHelper.GetSummonRecipeIdsWithGradeGuarantee(
                    summonRow, summonCount, random, costumeItemSheet, null);
            }
            else
            {
                // Use original random selection - don't pre-generate, process one by one
                recipeIds = null; // Will be processed one by one in the loop
            }

            if (summonRow.UseGradeGuarantee && recipeIds != null)
            {
                // Process pre-generated recipe IDs
                foreach (var recipeId in recipeIds)
                {
                    // Validate Recipe ResultEquipmentId
                    if (!costumeItemSheet.TryGetValue(recipeId,
                            out var costumeRow))
                    {
                        throw new SheetRowNotFoundException(
                            addressesHex,
                            nameof(CostumeItemSheet),
                            recipeId);
                    }

                    // Create Costume
                    var costume = ItemFactory.CreateCostume(
                        costumeRow,
                        random.GenerateRandomGuid()
                    );

                    result.Add(costume);
                }
            }
            else
            {
                // Original logic - process one by one
                for (var i = 0; i < summonCount; i++)
                {
                    var recipeId = SummonHelper.GetSummonRecipeIdByRandom(summonRow, random);

                    // Validate Recipe ResultEquipmentId
                    if (!costumeItemSheet.TryGetValue(recipeId,
                            out var costumeRow))
                    {
                        throw new SheetRowNotFoundException(
                            addressesHex,
                            nameof(CostumeItemSheet),
                            recipeId);
                    }

                    // Create Costume
                    var costume = ItemFactory.CreateCostume(
                        costumeRow,
                        random.GenerateRandomGuid()
                    );

                    result.Add(costume);
                }
            }

            return result;
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug($"{addressesHex} CostumeSummon Exec. Started.");

            var agentState = states.GetAgentState(context.Signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex} Aborted as the agent state of the signer was failed to load.");
            }

            if (!states.TryGetAvatarState(context.Signer, AvatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex} Aborted as the avatar state of the signer was failed to load.");
            }

            if (!SummonHelper.CheckSummonCountIsValid(SummonCount))
            {
                throw new InvalidSummonCountException(
                    $"{addressesHex} Given summonCount {SummonCount} is not valid. Please use 1 or 10 or 100."
                );
            }

            // Validate Work
            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(sheetTypes: new[]
            {
                typeof(CostumeSummonSheet),
                typeof(CostumeItemSheet),
                typeof(MaterialItemSheet),
            });

            var summonSheet = sheets.GetSheet<CostumeSummonSheet>();
            var materialSheet = sheets.GetSheet<MaterialItemSheet>();

            var summonRow = summonSheet.OrderedList.FirstOrDefault(row => row.GroupId == GroupId);
            if (summonRow is null)
            {
                throw new RowNotInTableException(
                    $"{addressesHex} Failed to get {GroupId} in CostumeSummonSheet");
            }

            // Use materials
            var inventory = avatarState.inventory;
            var material = materialSheet.OrderedList.First(m => m.Id == summonRow.CostMaterial);
            if (!inventory.RemoveFungibleItem(material.ItemId, context.BlockIndex,
                    summonRow.CostMaterialCount * SummonCount))
            {
                throw new NotEnoughMaterialException(
                    $"{addressesHex} Aborted as the player has no enough material ({summonRow.CostMaterial} * {summonRow.CostMaterialCount})");
            }

            // Transfer Cost NCG first for fast-fail
            if (summonRow.CostNcg > 0L)
            {
                var feeAddress = states.GetFeeAddress(context.BlockIndex);

                states = states.TransferAsset(
                    context,
                    context.Signer,
                    feeAddress,
                    states.GetGoldCurrency() * summonRow.CostNcg * SummonCount
                );
            }

            var random = context.GetRandom();
            var summonResult = SimulateSummon(
                addressesHex,
                sheets.GetSheet<CostumeItemSheet>(),
                summonRow,
                SummonCount,
                random
            );

            foreach (var costume in summonResult)
            {
                avatarState.UpdateFromAddCostume(costume);
            }

            Log.Debug(
                $"{addressesHex} CostumeSummon Exec. finished: {DateTimeOffset.UtcNow - started} Elapsed");

            avatarState.blockIndex = context.BlockIndex;
            avatarState.updatedAt = context.BlockIndex;

            // Set states
            return states
                .SetAvatarState(AvatarAddress, avatarState)
                .SetAgentState(context.Signer, agentState);
        }
    }
}
