using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Exceptions;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Nekoyume.Arena;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Summon;
using Serilog;

namespace Nekoyume.Action
{
    [ActionType("rune_summon")]
    public class RuneSummon : GameAction, IRuneSummonV1
    {
        public const string AvatarAddressKey = "aa";
        public Address AvatarAddress;

        public const string GroupIdKey = "gid";
        public int GroupId;

        public const string SummonCountKey = "sc";
        public int SummonCount;

        public const int RuneQuantity = 10;

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug($"{addressesHex} RuneSummon Exec. Started.");

            if (!states.TryGetAvatarState(context.Signer, AvatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex} Aborted as the avatar state of the signer was failed to load.");
            }

            if (!SummonHelper.CheckSummonCountIsValid(SummonCount))
            {
                throw new InvalidSummonCountException(
                    $"{addressesHex} Given summonCount {SummonCount} is not valid. Please use 1 or 10 or 100"
                );
            }

            // Validate Work
            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(sheetTypes: new[]
            {
                typeof(RuneSummonSheet),
                typeof(MaterialItemSheet),
                typeof(RuneSheet),
            });

            var summonSheet = sheets.GetSheet<RuneSummonSheet>();
            var materialSheet = sheets.GetSheet<MaterialItemSheet>();
            var runeSheet = sheets.GetSheet<RuneSheet>();

            var summonRow = summonSheet.OrderedList.FirstOrDefault(row => row.GroupId == GroupId);
            if (summonRow is null)
            {
                throw new RowNotInTableException(
                    $"{addressesHex} Failed to get {GroupId} in RuneSummonSheet");
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
            states = Summon(
                context,
                AvatarAddress,
                runeSheet,
                summonRow,
                SummonCount,
                random,
                states
            );

            Log.Debug(
                $"{addressesHex} RuneSummon Exec. finished: {DateTimeOffset.UtcNow - started} Elapsed");

            avatarState.blockIndex = context.BlockIndex;
            avatarState.updatedAt = context.BlockIndex;

            // Set states
            return states.SetAvatarState(AvatarAddress, avatarState);
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [AvatarAddressKey] = AvatarAddress.Serialize(),
                [GroupIdKey] = (Integer) GroupId,
                [SummonCountKey] = (Integer) SummonCount,
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
            GroupId = (Integer) plainValue[GroupIdKey];
            SummonCount = (Integer) plainValue[SummonCountKey];
        }

        Address IRuneSummonV1.AvatarAddress => AvatarAddress;
        int IRuneSummonV1.GroupId => GroupId;
        int IRuneSummonV1.SummonCount => SummonCount;

        public static IWorld Summon(
            IActionContext context,
            Address avatarAddress,
            RuneSheet runeSheet,
            SummonSheet.Row summonRow,
            int summonCount,
            IRandom random,
            IWorld states
        )
        {
            var result = SimulateSummon(runeSheet, summonRow, summonCount, random);
#pragma warning disable LAA1002
            foreach (var pair in result)
#pragma warning restore LAA1002
            {
                states = states.MintAsset(context, avatarAddress, pair.Key * pair.Value);
            }

            return states;
        }

        public static Dictionary<Currency, int> SimulateSummon(
            RuneSheet runeSheet,
            SummonSheet.Row summonRow,
            int summonCount,
            IRandom random
        )
        {
            summonCount = SummonHelper.CalculateSummonCount(summonCount);

            var result = new Dictionary<Currency, int>();
            for (var i = 0; i < summonCount; i++)
            {
                var recipeId = SummonHelper.GetSummonRecipeIdByRandom(summonRow, random);

                // Validate RecipeId
                var runeRow = runeSheet.OrderedList.FirstOrDefault(r => r.Id == recipeId);
                if (runeRow is null)
                {
                    throw new SheetRowNotFoundException(
                        nameof(RuneSheet),
                        recipeId
                    );
                }

                var ticker = runeRow.Ticker;
                var currency = Currencies.GetRune(ticker);
                result.TryAdd(currency, 0);
                result[currency] += RuneQuantity;
            }

            return result;
        }
    }
}
