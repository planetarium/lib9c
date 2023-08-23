using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Action.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Exceptions;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1991
    /// Updated at https://github.com/planetarium/lib9c/pull/1991
    /// </summary>
    [Serializable]
    [ActionType("create_avatar9")]
    public class CreateAvatar : GameAction, ICreateAvatarV2
    {
        public const string DeriveFormat = "avatar-state-{0}";

        public int index;
        public int hair;
        public int lens;
        public int ear;
        public int tail;
        public string name;

        int ICreateAvatarV2.Index => index;
        int ICreateAvatarV2.Hair => hair;
        int ICreateAvatarV2.Lens => lens;
        int ICreateAvatarV2.Ear => ear;
        int ICreateAvatarV2.Tail => tail;
        string ICreateAvatarV2.Name => name;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>()
        {
            ["index"] = (Integer) index,
            ["hair"] = (Integer) hair,
            ["lens"] = (Integer) lens,
            ["ear"] = (Integer) ear,
            ["tail"] = (Integer) tail,
            ["name"] = (Text) name,
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            index = (int) ((Integer) plainValue["index"]).Value;
            hair = (int) ((Integer) plainValue["hair"]).Value;
            lens = (int) ((Integer) plainValue["lens"]).Value;
            ear = (int) ((Integer) plainValue["ear"]).Value;
            tail = (int) ((Integer) plainValue["tail"]).Value;
            name = (Text) plainValue["name"];
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            IActionContext ctx = context;
            var signer = ctx.Signer;
            var world = ctx.PreviousState;
            var avatarAddress = signer.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DeriveFormat,
                    index
                )
            );
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            if (ctx.Rehearsal)
            {
                world = LegacyModule.SetState(world, signer, MarkChanged);
                for (var i = 0; i < AvatarState.CombinationSlotCapacity; i++)
                {
                    var slotAddress = avatarAddress.Derive(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            CombinationSlotState.DeriveFormat,
                            i
                        )
                    );
                    world = LegacyModule.SetState(world, slotAddress, MarkChanged);
                }

                world = LegacyModule.SetState(world, avatarAddress, MarkChanged);
                world = LegacyModule.SetState(world, inventoryAddress, MarkChanged);
                world = LegacyModule.SetState(world, worldInformationAddress, MarkChanged);
                world = LegacyModule.SetState(world, questListAddress, MarkChanged);
                world = LegacyModule.MarkBalanceChanged(world, ctx, GoldCurrencyMock, signer);
                return world;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            if (!Regex.IsMatch(name, GameConfig.AvatarNickNamePattern))
            {
                throw new InvalidNamePatternException(
                    $"{addressesHex}Aborted as the input name {name} does not follow the allowed name pattern.");
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}CreateAvatar exec started", addressesHex);
            AgentState existingAgentState = AgentModule.GetAgentState(world, signer);
            var agentState = existingAgentState ?? new AgentState(signer);
            var avatarState = AvatarModule.GetAvatarState(world, avatarAddress);
            if (!(avatarState is null))
            {
                throw new InvalidAddressException(
                    $"{addressesHex}Aborted as there is already an avatar at {avatarAddress}.");
            }

            if (!(0 <= index && index < GameConfig.SlotCount))
            {
                throw new AvatarIndexOutOfRangeException(
                    $"{addressesHex}Aborted as the index is out of range #{index}.");
            }

            if (agentState.avatarAddresses.ContainsKey(index))
            {
                throw new AvatarIndexAlreadyUsedException(
                    $"{addressesHex}Aborted as the signer already has an avatar at index #{index}.");
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}CreateAvatar Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            Log.Verbose("{AddressesHex}Execute CreateAvatar; player: {AvatarAddress}", addressesHex, avatarAddress);

            agentState.avatarAddresses.Add(index, avatarAddress);

            // Avoid NullReferenceException in test
            var materialItemSheet = LegacyModule.GetSheet<MaterialItemSheet>(ctx.PreviousState);

            avatarState = AvatarState.CreateAvatarState(name, avatarAddress, ctx, materialItemSheet, default);

            if (hair < 0) hair = 0;
            if (lens < 0) lens = 0;
            if (ear < 0) ear = 0;
            if (tail < 0) tail = 0;

            avatarState.Customize(hair, lens, ear, tail);

            foreach (var address in avatarState.combinationSlotAddresses)
            {
                var slotState =
                    new CombinationSlotState(
                        address,
                        GameConfig.RequireClearedStageLevel.CombinationEquipmentAction);
                world = LegacyModule.SetState(world, address, slotState.Serialize());
            }

            avatarState.UpdateQuestRewards(materialItemSheet);

            // Add Runes when executing on editor mode.
#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            states = CreateAvatar0.AddRunesForTest(ctx, avatarAddress, states);

            // Add pets for test
            if (states.TryGetSheet(out PetSheet petSheet))
            {
                foreach (var row in petSheet)
                {
                    var petState = new PetState(row.Id);
                    petState.LevelUp();
                    var petStateAddress = PetState.DeriveAddress(avatarAddress, row.Id);
                    states = states.SetState(petStateAddress, petState.Serialize());
                }
            }

            var recipeIds = new int[] {
                21,
                62,
                103,
                128,
                148,
                152,
            };
            var equipmentSheet = states.GetSheet<EquipmentItemSheet>();
            var recipeSheet = states.GetSheet<EquipmentItemRecipeSheet>();
            var subRecipeSheet = states.GetSheet<EquipmentItemSubRecipeSheetV2>();
            var optionSheet = states.GetSheet<EquipmentItemOptionSheet>();
            var skillSheet = states.GetSheet<SkillSheet>();
            var characterLevelSheet = states.GetSheet<CharacterLevelSheet>();
            var enhancementCostSheet = states.GetSheet<EnhancementCostSheetV2>();

            avatarState.level = 300;
            avatarState.exp = characterLevelSheet[300].Exp;

            // prepare equipments for test
            foreach (var recipeId in recipeIds)
            {
                var recipeRow = recipeSheet[recipeId];
                var subRecipeId = recipeRow.SubRecipeIds[1];
                var subRecipeRow = subRecipeSheet[subRecipeId];
                var equipmentRow = equipmentSheet[recipeRow.ResultEquipmentId];

                var equipment = (Equipment)ItemFactory.CreateItemUsable(
                    equipmentRow,
                    context.Random.GenerateRandomGuid(),
                    0L,
                    madeWithMimisbrunnrRecipe: subRecipeRow.IsMimisbrunnrSubRecipe ?? false);

                foreach (var option in subRecipeRow.Options)
                {
                    var optionRow = optionSheet[option.Id];
                    // Add stats.
                    if (optionRow.StatType != StatType.NONE)
                    {
                        var statMap = new DecimalStat(optionRow.StatType, optionRow.StatMax);
                        equipment.StatsMap.AddStatAdditionalValue(statMap.StatType, statMap.TotalValue);
                        equipment.optionCountFromCombination++;
                    }
                    // Add skills.
                    else
                    {
                        var skillRow = skillSheet.OrderedList.First(r => r.Id == optionRow.SkillId);
                        var skill = SkillFactory.Get(
                            skillRow,
                            optionRow.SkillDamageMax,
                            optionRow.SkillChanceMax,
                            optionRow.StatDamageRatioMax,
                            optionRow.ReferencedStatType);
                        if (skill != null)
                        {
                            equipment.Skills.Add(skill);
                            equipment.optionCountFromCombination++;
                        }
                    }
                }

                for (int i = 1; i <= 20; ++i)
                {
                    var subType = equipment.ItemSubType;
                    var grade = equipment.Grade;
                    var costRow = enhancementCostSheet.Values
                        .First(x => x.ItemSubType == subType &&
                                    x.Grade == grade &&
                                    x.Level == i);
                    equipment.LevelUp(ctx.Random, costRow, true);
                }

                avatarState.inventory.AddItem(equipment);
            }
#endif

            sw.Stop();
            Log.Verbose("{AddressesHex}CreateAvatar CreateAvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}CreateAvatar Total Executed Time: {Elapsed}", addressesHex, ended - started);
            // TODO delete check blockIndex hard-fork this action
            // Fix invalid mint crystal balance in internal network. main-net always mint 200_000
            var mintingValue = context.BlockIndex > 7_210_000L ? 200_000 : 600_000;
            world = AgentModule.SetAgentState(world, signer, agentState);
            world = LegacyModule.SetState(world, inventoryAddress, avatarState.inventory.Serialize());
            world = LegacyModule.SetState(
                world,
                worldInformationAddress,
                avatarState.worldInformation.Serialize());
            world = LegacyModule.SetState(world, questListAddress, avatarState.questList.Serialize());
            world = AvatarModule.SetAvatarStateV2(world, avatarAddress, avatarState);
            world = LegacyModule.MintAsset(world, ctx, signer, mintingValue * CrystalCalculator.CRYSTAL);
            return world;
        }
    }
}
