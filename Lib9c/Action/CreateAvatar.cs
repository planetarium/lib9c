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

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/2166
    /// Updated at https://github.com/planetarium/lib9c/pull/2166
    /// </summary>
    [Serializable]
    [ActionType("create_avatar10")]
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

                world = AvatarModule.MarkChanged(world, avatarAddress, true, true, true, true);
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
            var random = context.GetRandom();

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
                    random.GenerateRandomGuid(),
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
                    equipment.LevelUp(random, costRow, true);
                }

                avatarState.inventory.AddItem(equipment);
            }
#endif
            var sheets = ctx.PreviousState.GetSheets(containItemSheet: true,
                sheetTypes: new[] {typeof(CreateAvatarItemSheet), typeof(CreateAvatarFavSheet)});
            var itemSheet = sheets.GetItemSheet();
            var createAvatarItemSheet = sheets.GetSheet<CreateAvatarItemSheet>();
            AddItem(itemSheet, createAvatarItemSheet, avatarState, context.GetRandom());
            var createAvatarFavSheet = sheets.GetSheet<CreateAvatarFavSheet>();
            states = MintAsset(createAvatarFavSheet, avatarState, states, context);
            sw.Stop();
            Log.Verbose("{AddressesHex}CreateAvatar CreateAvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}CreateAvatar Total Executed Time: {Elapsed}", addressesHex, ended - started);

            world = AgentModule.SetAgentState(world, signer, agentState);
            world = AvatarModule.SetAvatarState(world, avatarAddress, avatarState);
            return world;
        }

        public static void AddItem(ItemSheet itemSheet, CreateAvatarItemSheet createAvatarItemSheet,
            AvatarState avatarState, IRandom random)
        {
            foreach (var row in createAvatarItemSheet.Values)
            {
                var itemId = row.ItemId;
                var count = row.Count;
                var itemRow = itemSheet[itemId];
                if (itemRow is MaterialItemSheet.Row materialRow)
                {
                    var item = ItemFactory.CreateMaterial(materialRow);
                    avatarState.inventory.AddItem(item, count);
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        var item = ItemFactory.CreateItem(itemRow, random);
                        avatarState.inventory.AddItem(item);
                    }
                }
            }
        }

        public static IAccount MintAsset(CreateAvatarFavSheet favSheet,
            AvatarState avatarState, IWorld world, IActionContext context)
        {
            foreach (var row in favSheet.Values)
            {
                var currency = row.Currency;
                var targetAddress = row.Target switch
                {
                    CreateAvatarFavSheet.Target.Agent => avatarState.agentAddress,
                    CreateAvatarFavSheet.Target.Avatar => avatarState.address,
                    _ => throw new ArgumentOutOfRangeException()
                };
                world = LegacyModule.MintAsset(world, context, targetAddress, currency * row.Quantity);
            }

            return world;
        }
    }
}
