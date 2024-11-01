using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Bencodex.Types;
using Lib9c;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
using Lib9c.DevExtensions;
using Lib9c.DevExtensions.Model;
using Nekoyume.Model.Skill;
#endif

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/2195
    /// </summary>
    [Serializable]
    [ActionType("create_avatar11")]
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

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>()
            {
                ["index"] = (Integer)index,
                ["hair"] = (Integer)hair,
                ["lens"] = (Integer)lens,
                ["ear"] = (Integer)ear,
                ["tail"] = (Integer)tail,
                ["name"] = (Text)name,
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            index = (int)((Integer)plainValue["index"]).Value;
            hair = (int)((Integer)plainValue["hair"]).Value;
            lens = (int)((Integer)plainValue["lens"]).Value;
            ear = (int)((Integer)plainValue["ear"]).Value;
            tail = (int)((Integer)plainValue["tail"]).Value;
            name = (Text)plainValue["name"];
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var ctx = context;
            var signer = ctx.Signer;
            var states = ctx.PreviousState;
            var avatarAddress = signer.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DeriveFormat,
                    index
                )
            );

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            ValidateName(addressesHex);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}CreateAvatar exec started", addressesHex);

            var agentState = GetAgentState(states, signer, avatarAddress, addressesHex);

            sw.Stop();
            Log.Verbose("{AddressesHex}CreateAvatar Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            Log.Verbose("{AddressesHex}Execute CreateAvatar; player: {AvatarAddress}", addressesHex, avatarAddress);

            agentState.avatarAddresses.Add(index, avatarAddress);

            // Avoid NullReferenceException in test
            var materialItemSheet = ctx.PreviousState.GetSheet<MaterialItemSheet>();
            var avatarState = CreateAvatarState(name, avatarAddress, ctx, materialItemSheet, default);

            CustomizeAvatar(avatarState);

            var allCombinationSlotState = CreateCombinationSlots(avatarAddress);
            states = states.SetCombinationSlotState(avatarAddress, allCombinationSlotState);

            avatarState.UpdateQuestRewards(materialItemSheet);

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            states = ExecuteDevExtensions(ctx, avatarAddress, states, avatarState);
#endif

            var sheets = ctx.PreviousState.GetSheets(containItemSheet: true,
                sheetTypes: new[]
                {
                    typeof(CreateAvatarItemSheet), typeof(CreateAvatarFavSheet),
                });
            var itemSheet = sheets.GetItemSheet();
            var createAvatarItemSheet = sheets.GetSheet<CreateAvatarItemSheet>();
            AddItem(itemSheet, createAvatarItemSheet, avatarState, context.GetRandom());
            var createAvatarFavSheet = sheets.GetSheet<CreateAvatarFavSheet>();
            states = MintAsset(createAvatarFavSheet, avatarState, states, context);

            sw.Stop();
            Log.Verbose("{AddressesHex}CreateAvatar CreateAvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}CreateAvatar Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetAgentState(signer, agentState)
                .SetAvatarState(avatarAddress, avatarState)
                .SetActionPoint(avatarAddress, DailyReward.ActionPointMax)
                .SetDailyRewardReceivedBlockIndex(avatarAddress, 0L);
        }

        private void ValidateName(string addressesHex)
        {
            if (!Regex.IsMatch(name, GameConfig.AvatarNickNamePattern))
            {
                throw new InvalidNamePatternException(
                    $"{addressesHex}Aborted as the input name {name} does not follow the allowed name pattern.");
            }
        }

        private AgentState GetAgentState(IWorld states, Address signer, Address avatarAddress, string addressesHex)
        {
            var existingAgentState = states.GetAgentState(signer);
            var agentState = existingAgentState ?? new AgentState(signer);
            // check has avatar in avatarAddress, see InvalidAddressException in this method
            var avatarState = states.GetAvatarState(avatarAddress, false, false, false);
            if (avatarState is not null)
            {
                throw new InvalidAddressException(
                    $"{addressesHex}Aborted as there is already an avatar at {avatarAddress}.");
            }

            if (index is < 0 or >= GameConfig.SlotCount)
            {
                throw new AvatarIndexOutOfRangeException(
                    $"{addressesHex}Aborted as the index is out of range #{index}.");
            }

            if (agentState.avatarAddresses.ContainsKey(index))
            {
                throw new AvatarIndexAlreadyUsedException(
                    $"{addressesHex}Aborted as the signer already has an avatar at index #{index}.");
            }

            return agentState;
        }

        private void CustomizeAvatar(AvatarState avatarState)
        {
            if (hair < 0)
            {
                hair = 0;
            }

            if (lens < 0)
            {
                lens = 0;
            }

            if (ear < 0)
            {
                ear = 0;
            }

            if (tail < 0)
            {
                tail = 0;
            }

            avatarState.Customize(hair, lens, ear, tail);
        }

        private AllCombinationSlotState CreateCombinationSlots(Address avatarAddress)
        {
            var allCombinationSlotState = new AllCombinationSlotState();
            for (var i = 0; i < AvatarState.DefaultCombinationSlotCount; i++)
            {
                var slotAddr = Addresses.GetCombinationSlotAddress(avatarAddress, i);
                var slot = new CombinationSlotState(slotAddr, i);
                allCombinationSlotState.AddSlot(slot);
            }

            return allCombinationSlotState;
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
                    for (var i = 0; i < count; i++)
                    {
                        var item = ItemFactory.CreateItem(itemRow, random);
                        avatarState.inventory.AddItem(item);
                    }
                }
            }
        }

        public static IWorld MintAsset(CreateAvatarFavSheet favSheet,
            AvatarState avatarState, IWorld states, IActionContext context)
        {
            foreach (var row in favSheet.Values)
            {
                var currency = row.Currency;
                var targetAddress = row.Target switch
                {
                    CreateAvatarFavSheet.Target.Agent => avatarState.agentAddress,
                    CreateAvatarFavSheet.Target.Avatar => avatarState.address,
                    _ => throw new ArgumentOutOfRangeException(),
                };
                states = states.MintAsset(context, targetAddress, currency * row.Quantity);
            }

            return states;
        }

        public static AvatarState CreateAvatarState(string name,
            Address avatarAddress,
            IActionContext ctx,
            MaterialItemSheet materialItemSheet,
            Address rankingMapAddress)
        {
            var state = ctx.PreviousState;
            var random = ctx.GetRandom();
            var avatarState = AvatarState.Create(
                avatarAddress,
                ctx.Signer,
                ctx.BlockIndex,
                state.GetAvatarSheets(),
                rankingMapAddress,
                name
            );

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            AddTestItems(ctx, avatarState, random, materialItemSheet);
#endif

            return avatarState;
        }

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
        private static IWorld ExecuteDevExtensions(IActionContext ctx, Address avatarAddress, IWorld states, AvatarState avatarState)
        {
            // prepare for test when executing on editor mode.
            var data = TestbedHelper.LoadData<TestbedCreateAvatar>("TestbedCreateAvatar");

            states = AddRunesForTest(ctx, avatarAddress, states, data.RuneStoneCount);
            states = AddSoulStoneForTest(ctx, avatarAddress, states, data.SoulStoneCount);
            if (data.AddPet)
            {
                states = AddPetsForTest(avatarAddress, states);
            }

            var equipmentSheet = states.GetSheet<EquipmentItemSheet>();
            var recipeSheet = states.GetSheet<EquipmentItemRecipeSheet>();
            var subRecipeSheet = states.GetSheet<EquipmentItemSubRecipeSheetV2>();
            var optionSheet = states.GetSheet<EquipmentItemOptionSheet>();
            var skillSheet = states.GetSheet<SkillSheet>();
            var characterLevelSheet = states.GetSheet<CharacterLevelSheet>();
            var enhancementCostSheet = states.GetSheet<EnhancementCostSheetV2>();
            var random = ctx.GetRandom();

            avatarState.level = data.Level;
            avatarState.exp = characterLevelSheet[data.Level].Exp;

            foreach (var recipeId in data.FullOptionEquipmentRecipeIds)
            {
                AddFullOptionEquipment(
                    avatarState,
                    random,
                    equipmentSheet,
                    recipeSheet,
                    subRecipeSheet,
                    optionSheet,
                    skillSheet,
                    enhancementCostSheet,
                    recipeId);
            }

            return states;
        }

        private static void AddFullOptionEquipment(
            AvatarState avatarState,
            IRandom random,
            EquipmentItemSheet equipmentSheet,
            EquipmentItemRecipeSheet recipeSheet,
            EquipmentItemSubRecipeSheetV2 subRecipeSheet,
            EquipmentItemOptionSheet optionSheet,
            SkillSheet skillSheet,
            EnhancementCostSheetV2 enhancementCostSheet,
            int recipeId)
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
                AddOptionToEquipment(equipment, optionRow, skillSheet);
            }

            EnhanceEquipmentToMaxLevel(equipment, enhancementCostSheet, random);

            avatarState.inventory.AddItem(equipment);
        }

        private static void AddOptionToEquipment(Equipment equipment, EquipmentItemOptionSheet.Row optionRow, SkillSheet skillSheet)
        {
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

        private static void EnhanceEquipmentToMaxLevel(Equipment equipment, EnhancementCostSheetV2 enhancementCostSheet, IRandom random)
        {
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
        }

        private static void AddTestItems(
            IActionContext ctx,
            AvatarState avatarState,
            IRandom random,
            MaterialItemSheet materialItemSheet)
        {
            var data = TestbedHelper.LoadData<TestbedCreateAvatar>("TestbedCreateAvatar");
            var costumeItemSheet = ctx.PreviousState.GetSheet<CostumeItemSheet>();
            var equipmentItemSheet = ctx.PreviousState.GetSheet<EquipmentItemSheet>();
            var consumableItemSheet = ctx.PreviousState.GetSheet<ConsumableItemSheet>();

            AddItemsForTest(
                avatarState: avatarState,
                random: random,
                costumeItemSheet: costumeItemSheet,
                materialItemSheet: materialItemSheet,
                equipmentItemSheet: equipmentItemSheet,
                consumableItemSheet: consumableItemSheet,
                data.MaterialCount,
                data.TradableMaterialCount,
                data.FoodCount);

            var skillSheet = ctx.PreviousState.GetSheet<SkillSheet>();
            var optionSheet = ctx.PreviousState.GetSheet<EquipmentItemOptionSheet>();

            var items = data.CustomEquipmentItems;
            foreach (var item in items)
            {
                AddCustomEquipment(
                    avatarState: avatarState,
                    random: random,
                    skillSheet: skillSheet,
                    equipmentItemSheet: equipmentItemSheet,
                    equipmentItemOptionSheet: optionSheet,
                    // Set level of equipment here.
                    level: item.Level,
                    // Set recipeId of target equipment here.
                    recipeId: item.ID,
                    // Add optionIds here.
                    item.OptionIds);
            }
        }

        private static IWorld AddRunesForTest(
            IActionContext context,
            Address avatarAddress,
            IWorld states,
            int count = int.MaxValue)
        {
            var runeSheet = states.GetSheet<RuneSheet>();
            foreach (var row in runeSheet.Values)
            {
                var rune = RuneHelper.ToFungibleAssetValue(row, count);
                states = states.MintAsset(context, avatarAddress, rune);
            }
            return states;
        }

        private static IWorld AddSoulStoneForTest(
            IActionContext context,
            Address avatarAddress,
            IWorld states,
            int count = int.MaxValue)
        {
            var petSheet = states.GetSheet<PetSheet>();
            foreach (var row in petSheet.Values)
            {
                var soulStone = Currencies.GetSoulStone(row.SoulStoneTicker) * count;
                states = states.MintAsset(context, avatarAddress, soulStone);
            }
            return states;
        }

        private static IWorld AddPetsForTest(
            Address avatarAddress,
            IWorld states)
        {
            var petSheet = states.GetSheet<PetSheet>();
            foreach (var id in petSheet.Keys)
            {
                var petState = new PetState(id);
                petState.LevelUp();
                var petStateAddress = PetState.DeriveAddress(avatarAddress, id);
                states = states.SetLegacyState(petStateAddress, petState.Serialize());
            }

            return states;
        }

        private static void AddItemsForTest(
            AvatarState avatarState,
            IRandom random,
            CostumeItemSheet costumeItemSheet,
            MaterialItemSheet materialItemSheet,
            EquipmentItemSheet equipmentItemSheet,
            ConsumableItemSheet consumableItemSheet,
            int materialCount,
            int tradableMaterialCount,
            int foodCount)
        {
            foreach (var row in costumeItemSheet.OrderedList)
            {
                avatarState.inventory.AddItem2(ItemFactory.CreateCostume(row, random.GenerateRandomGuid()));
            }

            foreach (var row in materialItemSheet.OrderedList)
            {
                avatarState.inventory.AddItem2(ItemFactory.CreateMaterial(row), materialCount);

                if (row.ItemSubType == ItemSubType.Hourglass ||
                    row.ItemSubType == ItemSubType.ApStone)
                {
                    avatarState.inventory.AddItem2(ItemFactory.CreateTradableMaterial(row), tradableMaterialCount);
                }
            }

            foreach (var row in equipmentItemSheet.OrderedList.Where(row =>
                row.Id > GameConfig.DefaultAvatarWeaponId))
            {
                var itemId = random.GenerateRandomGuid();
                avatarState.inventory.AddItem2(ItemFactory.CreateItemUsable(row, itemId, default));
            }

            foreach (var row in consumableItemSheet.OrderedList)
            {
                for (var i = 0; i < foodCount; i++)
                {
                    var itemId = random.GenerateRandomGuid();
                    var consumable = (Consumable)ItemFactory.CreateItemUsable(row, itemId,
                        0, 0);
                    avatarState.inventory.AddItem2(consumable);
                }
            }
        }

        private static void AddCustomEquipment(
            AvatarState avatarState,
            IRandom random,
            SkillSheet skillSheet,
            EquipmentItemSheet equipmentItemSheet,
            EquipmentItemOptionSheet equipmentItemOptionSheet,
            int level,
            int recipeId,
            params int[] optionIds
            )
        {
            if (!equipmentItemSheet.TryGetValue(recipeId, out var equipmentRow))
            {
                return;
            }

            var itemId = random.GenerateRandomGuid();
            var equipment = (Equipment)ItemFactory.CreateItemUsable(equipmentRow, itemId, 0, level);
            var optionRows = new List<EquipmentItemOptionSheet.Row>();
            foreach (var optionId in optionIds)
            {
                if (!equipmentItemOptionSheet.TryGetValue(optionId, out var optionRow))
                {
                    continue;
                }
                optionRows.Add(optionRow);
            }

            AddOption(skillSheet, equipment, optionRows, random);

            avatarState.inventory.AddItem2(equipment);
        }

        private static HashSet<int> AddOption(
            SkillSheet skillSheet,
            Equipment equipment,
            IEnumerable<EquipmentItemOptionSheet.Row> optionRows,
            IRandom random)
        {
            var optionIds = new HashSet<int>();

            foreach (var optionRow in optionRows.OrderBy(r => r.Id))
            {
                if (optionRow.StatType != StatType.NONE)
                {
                    var stat = CombinationEquipment5.GetStat(optionRow, random);
                    equipment.StatsMap.AddStatAdditionalValue(stat.StatType, stat.BaseValue);
                }
                else
                {
                    var skill = CombinationEquipment5.GetSkill(optionRow, skillSheet, random);
                    if (!(skill is null))
                    {
                        equipment.Skills.Add(skill);
                    }
                }

                optionIds.Add(optionRow.Id);
            }

            return optionIds;
        }
#endif
    }
}
