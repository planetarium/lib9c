#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Bencodex.Types;
using Lib9c.DevExtensions.Action.Interface;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.Quest;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;

namespace Lib9c.DevExtensions.Action
{
    [Serializable]
    [ActionType("create_or_replace_avatar")]
    // Don't use on client
    public class CreateOrReplaceAvatar : GameAction, ICreateOrReplaceAvatar
    {
        public int AvatarIndex { get; private set; }
        public string Name { get; private set; }
        public int Hair { get; private set; }
        public int Lens { get; private set; }
        public int Ear { get; private set; }
        public int Tail { get; private set; }
        public int Level { get; private set; }
        public IOrderedEnumerable<(int equipmentId, int level)> Equipments { get; private set; }
        public IOrderedEnumerable<(int consumableId, int count)> Foods { get; private set; }
        public IOrderedEnumerable<int> CostumeIds { get; private set; }
        public IOrderedEnumerable<(int runeId, int level)> Runes { get; private set; }

        public (int stageId, IOrderedEnumerable<int> crystalRandomBuffIds)? CrystalRandomBuff { get; private set; }
        // public

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var list = new List(
                    (Integer)AvatarIndex,
                    (Text)Name,
                    (Integer)Hair,
                    (Integer)Lens,
                    (Integer)Ear,
                    (Integer)Tail,
                    (Integer)Level,
                    new List(Equipments.Select(e => new List(
                        (Integer)e.equipmentId,
                        (Integer)e.level))),
                    new List(Foods.Select(e => new List(
                        (Integer)e.consumableId,
                        (Integer)e.count))),
                    new List(CostumeIds.Select(e => (Integer)e)),
                    new List(Runes.Select(e => new List(
                        (Integer)e.runeId,
                        (Integer)e.level))),
                    CrystalRandomBuff is null
                        ? (IValue)Null.Value
                        : new List(
                            (Integer)CrystalRandomBuff.Value.stageId,
                            new List(CrystalRandomBuff.Value.crystalRandomBuffIds.Select(e =>
                                (Integer)e))));
                return new Dictionary<string, IValue>
                {
                    ["l"] = list,
                }.ToImmutableDictionary();
            }
        }

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            var list = (List)plainValue["l"];
            AvatarIndex = (Integer)list[0];
            Name = (Text)list[1];
            Hair = (Integer)list[2];
            Lens = (Integer)list[3];
            Ear = (Integer)list[4];
            Tail = (Integer)list[5];
            Level = (Integer)list[6];
            Equipments = ((List)list[7])
                .OfType<List>()
                .Select<List, (int equipmentId, int level)>(l => ((Integer)l[0], (Integer)l[1]))
                .OrderBy(tuple => tuple.equipmentId)
                .ThenBy(tuple => tuple.level);
            Foods = ((List)list[8])
                .OfType<List>()
                .Select<List, (int consumableId, int count)>(l => ((Integer)l[0], (Integer)l[1]))
                .OrderBy(tuple => tuple.consumableId)
                .ThenBy(tuple => tuple.count);
            CostumeIds = ((List)list[9])
                .Select(i => (int)(Integer)i)
                .OrderBy(id => id);
            Runes = ((List)list[10])
                .OfType<List>()
                .Select<List, (int runeId, int level)>(l => ((Integer)l[0], (Integer)l[1]))
                .OrderBy(tuple => tuple.runeId)
                .ThenBy(tuple => tuple.level);
            CrystalRandomBuff = list[11] is List l11
                ? (
                    (Integer)l11[0],
                    ((List)l11[1])
                    .Select(crystalRandomBuffId => (int)(Integer)crystalRandomBuffId)
                    .OrderBy(crystalRandomBuffId => crystalRandomBuffId))
                : ((int stageId, IOrderedEnumerable<int> crystalRandomBuffIds)?)null;
        }

        public CreateOrReplaceAvatar() :
            this(equipments: ((int equipmentId, int level)[]?)null)
        {
        }

        public CreateOrReplaceAvatar(
            int avatarIndex = 0,
            string name = "Avatar",
            int hair = 0,
            int lens = 0,
            int ear = 0,
            int tail = 0,
            int level = 1,
            (int equipmentId, int level)[]? equipments = null,
            (int consumableId, int count)[]? foods = null,
            int[]? costumeIds = null,
            (int runeId, int level)[]? runes = null,
            (int stageId, int[] crystalRandomBuffIds)? crystalRandomBuff = null) :
            this(
                avatarIndex,
                name,
                hair,
                lens,
                ear,
                tail,
                level,
                (equipments ?? Array.Empty<(int equipmentId, int level)>())
                .OrderBy(tuple => tuple.equipmentId)
                .ThenBy(tuple => tuple.level),
                (foods ?? Array.Empty<(int consumableId, int count)>())
                .OrderBy(tuple => tuple.consumableId)
                .ThenBy(tuple => tuple.count),
                (costumeIds ?? Array.Empty<int>())
                .OrderBy(id => id),
                (runes ?? Array.Empty<(int runeId, int level)>())
                .OrderBy(tuple => tuple.runeId)
                .ThenBy(tuple => tuple.level),
                crystalRandomBuff is null
                    ? ((int stageId, IOrderedEnumerable<int> crystalRandomBuffIds)?)null
                    : (crystalRandomBuff.Value.stageId,
                        crystalRandomBuff.Value.crystalRandomBuffIds.OrderBy(e => e)))
        {
        }

        public CreateOrReplaceAvatar(
            int avatarIndex = 0,
            string name = "Avatar",
            int hair = 0,
            int lens = 0,
            int ear = 0,
            int tail = 0,
            int level = 1,
            IOrderedEnumerable<(int equipmentId, int level)>? equipments = null,
            IOrderedEnumerable<(int consumableId, int count)>? foods = null,
            IOrderedEnumerable<int>? costumeIds = null,
            IOrderedEnumerable<(int runeId, int level)>? runes = null,
            (int stageId, IOrderedEnumerable<int> crystalRandomBuffIds)? crystalRandomBuff = null)
        {
            if (avatarIndex < 0 ||
                avatarIndex >= GameConfig.SlotCount)
            {
                throw new ArgumentException(
                    $"Invalid avatarIndex: ({avatarIndex})." +
                    $" It must be between 0 and {GameConfig.SlotCount - 1}.",
                    nameof(avatarIndex));
            }

            if (!Regex.IsMatch(name, GameConfig.AvatarNickNamePattern))
            {
                throw new ArgumentException(
                    $"Invalid nickname: \"{name}\"." +
                    " Nickname must be between 2 and 20" +
                    " characters long and can only contain alphabets and numbers.",
                    nameof(name));
            }

            if (hair < 0)
            {
                throw new ArgumentException(
                    $"Invalid hair: ({hair})." +
                    " It must be greater than or equal to 0.",
                    nameof(hair));
            }

            if (lens < 0)
            {
                throw new ArgumentException(
                    $"Invalid lens: ({lens})." +
                    " It must be greater than or equal to 0.",
                    nameof(lens));
            }

            if (ear < 0)
            {
                throw new ArgumentException(
                    $"Invalid ear: ({ear})." +
                    " It must be greater than or equal to 0.",
                    nameof(ear));
            }

            if (tail < 0)
            {
                throw new ArgumentException(
                    $"Invalid tail: ({tail})." +
                    " It must be greater than or equal to 0.",
                    nameof(tail));
            }

            if (level < 1)
            {
                throw new ArgumentException(
                    $"Invalid level: ({level})." +
                    " It must be greater than or equal to 1.",
                    nameof(level));
            }

            if (equipments != null)
            {
                foreach (var tuple in equipments)
                {
                    var (equipmentId, eLevel) = tuple;
                    if (equipmentId < 0)
                    {
                        throw new ArgumentException(
                            $"Invalid itemId: ({equipmentId})." +
                            " It must be greater than or equal to 0.",
                            nameof(equipmentId));
                    }

                    if (eLevel < 0)
                    {
                        throw new ArgumentException(
                            $"Invalid enhancement: ({eLevel})." +
                            " It must be greater than or equal to 0.",
                            nameof(eLevel));
                    }
                }
            }

            if (foods != null)
            {
                foreach (var tuple in foods)
                {
                    var (consumableId, count) = tuple;
                    if (consumableId < 0)
                    {
                        throw new ArgumentException(
                            $"Invalid itemId: ({consumableId})." +
                            " It must be greater than or equal to 0.",
                            nameof(consumableId));
                    }

                    if (count < 1)
                    {
                        throw new ArgumentException(
                            $"Invalid count: ({count})." +
                            " It must be greater than or equal to 0.",
                            nameof(count));
                    }
                }
            }

            if (costumeIds != null)
            {
                foreach (var costumeId in costumeIds)
                {
                    if (costumeId < 0)
                    {
                        throw new ArgumentException(
                            $"Invalid costumeId: ({costumeId})." +
                            " It must be greater than or equal to 0.",
                            nameof(costumeId));
                    }
                }
            }

            if (runes != null)
            {
                foreach (var tuple in runes)
                {
                    var (runeId, runeLevel) = tuple;
                    if (runeId < 0)
                    {
                        throw new ArgumentException(
                            $"Invalid itemId: ({runeId})." +
                            " It must be greater than or equal to 0.",
                            nameof(runeId));
                    }

                    if (runeLevel < 0)
                    {
                        throw new ArgumentException(
                            $"Invalid enhancement: ({runeLevel})." +
                            " It must be greater than or equal to 0.",
                            nameof(runeLevel));
                    }
                }
            }

            if (crystalRandomBuff != null)
            {
                if (crystalRandomBuff.Value.stageId < 0)
                {
                    throw new ArgumentException(
                        $"Invalid stageId: ({crystalRandomBuff.Value.stageId})." +
                        " It must be greater than or equal to 0.",
                        nameof(crystalRandomBuff));
                }

                foreach (var buffId in crystalRandomBuff.Value.crystalRandomBuffIds)
                {
                    if (buffId < 0)
                    {
                        throw new ArgumentException(
                            $"Invalid buffId: ({buffId})." +
                            " It must be greater than or equal to 0.",
                            nameof(buffId));
                    }
                }
            }

            AvatarIndex = avatarIndex;
            Name = name;
            Hair = hair;
            Lens = lens;
            Ear = ear;
            Tail = tail;
            Level = level;
            Equipments = equipments ?? Array.Empty<(int equipmentId, int level)>()
                .OrderBy(tuple => tuple.equipmentId)
                .ThenBy(tuple => tuple.level);
            Foods = foods ?? Array.Empty<(int consumableId, int count)>()
                .OrderBy(tuple => tuple.consumableId)
                .ThenBy(tuple => tuple.count);
            CostumeIds = costumeIds ?? Array.Empty<int>()
                .OrderBy(id => id);
            Runes = runes ?? Array.Empty<(int runeId, int level)>()
                .OrderBy(tuple => tuple.runeId)
                .ThenBy(tuple => tuple.level);
            CrystalRandomBuff = crystalRandomBuff;
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var random = context.GetRandom();
            return Execute(
                context.PreviousState,
                random,
                context.BlockIndex,
                context.Signer);
        }

        public IWorld Execute(
            IWorld states,
            IRandom random,
            long blockIndex,
            Address signer)
        {
            var agentAddr = signer;
            var avatarAddr = Addresses.GetAvatarAddress(agentAddr, AvatarIndex);

            // Set AgentState.
            var agent = states.GetLegacyState(agentAddr) is Dictionary agentDict
                ? new AgentState(agentDict)
                : new AgentState(agentAddr);
            if (!agent.avatarAddresses.ContainsKey(AvatarIndex))
            {
                agent.avatarAddresses[AvatarIndex] = avatarAddr;
                states = states.SetAgentState(agentAddr, agent);
            }
            // ~Set AgentState.

            var sheets = states.GetSheets(
                true,
                containQuestSheet: true,
                sheetTypes: new[]
                {
                    typeof(WorldSheet),
                    typeof(EquipmentItemRecipeSheet),
                    typeof(EquipmentItemSubRecipeSheet),
                    typeof(EquipmentItemSheet),
                    typeof(EnhancementCostSheetV2),
                    typeof(EquipmentItemRecipeSheet),
                    typeof(EquipmentItemSubRecipeSheetV2),
                    typeof(EquipmentItemOptionSheet),
                    typeof(SkillSheet),
                    typeof(ConsumableItemSheet),
                    typeof(CostumeItemSheet),
                    typeof(CrystalStageBuffGachaSheet),
                });

            // Set AvatarState.
            var avatar = AvatarState.Create(
                avatarAddr,
                agentAddr,
                blockIndex,
                sheets.GetAvatarSheets(),
                default,
                Name);
            avatar.level = Level;
            avatar.hair = Hair;
            avatar.lens = Lens;
            avatar.ear = Ear;
            avatar.tail = Tail;
            // ~Set AvatarState.

            // Set WorldInformation.
            avatar.worldInformation = new WorldInformation(
                blockIndex,
                sheets.GetSheet<WorldSheet>(),
                false);
            // ~Set WorldInformation.

            // Set QuestList.
            avatar.questList = new QuestList(
                sheets.GetQuestSheet(),
                sheets.GetSheet<QuestRewardSheet>(),
                sheets.GetSheet<QuestItemRewardSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheet>());
            // ~Set QuestList.

            // Set Inventory.
            var equipmentItemSheet = sheets.GetSheet<EquipmentItemSheet>();
            var enhancementCostSheetV2 = sheets.GetSheet<EnhancementCostSheetV2>();
            var recipeSheet = sheets.GetSheet<EquipmentItemRecipeSheet>();
            var subRecipeSheetV2 = sheets.GetSheet<EquipmentItemSubRecipeSheetV2>();
            var optionSheet = sheets.GetSheet<EquipmentItemOptionSheet>();
            var skillSheet = sheets.GetSheet<SkillSheet>();

            // FIXME: Separate the logic of equipment creation from the action.
            foreach (var (equipmentId, eLevel) in Equipments)
            {
                if (!equipmentItemSheet.TryGetValue(equipmentId, out var itemRow, true))
                {
                    continue;
                }

                // NOTE: Do not use `level` argument at here.
                var equipment = (Equipment)ItemFactory.CreateItemUsable(
                    itemRow,
                    random.GenerateRandomGuid(),
                    blockIndex);
                if (equipment.Grade == 0)
                {
                    avatar.inventory.AddItem(equipment);
                    continue;
                }

                var recipe = recipeSheet.OrderedList!
                    .First(e => e.ResultEquipmentId == equipmentId);
                var subRecipe = subRecipeSheetV2[recipe.SubRecipeIds[1]];
                CombinationEquipment.AddAndUnlockOption(
                    agent,
                    null,
                    equipment,
                    random,
                    subRecipe,
                    optionSheet,
                    null,
                    skillSheet);
                var additionalOptionStats = equipment.StatsMap.GetAdditionalStats(false).ToArray();
                foreach (var statMapEx in additionalOptionStats)
                {
                    equipment.StatsMap.SetStatAdditionalValue(statMapEx.statType, 0);
                }

                equipment.Skills.Clear();
                equipment.BuffSkills.Clear();

                var options = subRecipe.Options
                    .Select(e => optionSheet[e.Id])
                    .ToArray();
                foreach (var option in options)
                {
                    if (option.StatType == StatType.NONE)
                    {
                        var skillRow = skillSheet[option.SkillId];
                        var skill = SkillFactory.GetV1(
                            skillRow,
                            option.SkillDamageMax,
                            option.SkillChanceMax);
                        equipment.Skills.Add(skill);

                        continue;
                    }

                    equipment.StatsMap.AddStatAdditionalValue(option.StatType, option.StatMax);
                }

                if (eLevel > 0 &&
                    ItemEnhancement11.TryGetRow(
                        equipment,
                        enhancementCostSheetV2,
                        out var enhancementCostRow))
                {
                    for (var j = 0; j < eLevel; j++)
                    {
                        equipment.LevelUp(random, enhancementCostRow, true);
                    }
                }

                avatar.inventory.AddItem(equipment);
            }

            var consumableItemSheet = sheets.GetSheet<ConsumableItemSheet>();
            foreach (var (consumableId, count) in Foods)
            {
                if (consumableItemSheet.TryGetValue(consumableId, out var consumableRow))
                {
                    for (var i = 0; i < count; i++)
                    {
                        avatar.inventory.AddItem(ItemFactory.CreateItemUsable(
                            consumableRow,
                            random.GenerateRandomGuid(),
                            blockIndex));
                    }
                }
            }

            var costumeItemSheet = sheets.GetSheet<CostumeItemSheet>();
            foreach (var costumeId in CostumeIds)
            {
                if (costumeItemSheet.TryGetValue(costumeId, out var costumeRow))
                {
                    avatar.inventory.AddItem(ItemFactory.CreateCostume(
                        costumeRow,
                        random.GenerateRandomGuid()));
                }
            }
            // ~Set Inventory.

            states = states.SetAvatarState(avatarAddr, avatar);

            // Set CombinationSlot.
            var allCombinationSlotState = new AllCombinationSlotState();
            for (var i = 0; i < AvatarState.DefaultCombinationSlotCount; i++)
            {
                var slotAddr = Addresses.GetCombinationSlotAddress(avatarAddr, i);
                var slot = new CombinationSlotState(slotAddr, i);
                allCombinationSlotState.AddSlot(slot);
            }

            states = states.SetCombinationSlotState(avatarAddr, allCombinationSlotState);
            // ~Set CombinationSlot.

            // Set Runes
            var allRuneState = new AllRuneState();
            foreach (var (runeId, level) in Runes)
            {
                allRuneState.AddRuneState(new RuneState(runeId, level));
            }

            states = states.SetRuneState(avatarAddr, allRuneState);
            // ~Set Runes

            // Set CrystalRandomBuffState
            var crystalRandomSkillAddr =
                Addresses.GetSkillStateAddressFromAvatarAddress(avatarAddr);
            if (CrystalRandomBuff is null)
            {
                states = states.RemoveLegacyState(crystalRandomSkillAddr);
            }
            else
            {
                var crb = CrystalRandomBuff.Value;
                var crystalRandomSkillState = new CrystalRandomSkillState(
                    crystalRandomSkillAddr,
                    crb.stageId);
                var crystalStageBuffGachaSheet = sheets.GetSheet<CrystalStageBuffGachaSheet>();
                if (crystalStageBuffGachaSheet.TryGetValue(
                    crb.stageId,
                    out var crystalStageBuffGachaRow))
                {
                    crystalRandomSkillState.Update(
                        crystalStageBuffGachaRow.MaxStar,
                        crystalStageBuffGachaSheet);
                }

                crystalRandomSkillState.Update(crb.crystalRandomBuffIds.ToList());
                states = states.SetLegacyState(crystalRandomSkillAddr,
                    crystalRandomSkillState.Serialize());
            }
            // ~Set CrystalRandomBuffState

            return states;
        }
    }
}
