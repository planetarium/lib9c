using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// Agent가 포함하는 각 Avatar의 상태 모델이다.
    /// </summary>
    [Serializable]
    public class AvatarState : State, ICloneable
    {
        public const int DefaultCombinationSlotCount = 4;

        public const int CombinationSlotCapacity = 8;
        public const int CurrentVersion = 2;
        public string name;
        public int characterId;
        public int level;
        public long exp;
        public Inventory inventory;
        public WorldInformation worldInformation;
        // FIXME: it seems duplicated with blockIndex.
        public long updatedAt;
        public Address agentAddress;
        public QuestList questList;
        public MailBox mailBox;
        public long blockIndex;
        public long dailyRewardReceivedIndex;
        public int actionPoint;
        public CollectionMap stageMap;
        public CollectionMap monsterMap;
        public CollectionMap itemMap;
        public CollectionMap eventMap;
        public int hair;
        public int lens;
        public int ear;
        public int tail;
        [Obsolete("don't use this field, use AllCombinationSlotState instead.")]
        public List<Address> combinationSlotAddresses;

        public string NameWithHash { get; private set; }

        public int Nonce { get; private set; }

        public int Version { get; private set; }


        [Obsolete("don't use this field.")]
        public readonly Address RankingMapAddress;

        public static Address CreateAvatarAddress()
        {
            var key = new PrivateKey();
            return key.PublicKey.Address;
        }

        public static AvatarState Create(Address address,
            Address agentAddress,
            long blockIndex,
            AvatarSheets avatarSheets,
            Address rankingMapAddress,
            string name = null)
        {
            var worldInformationVar = new WorldInformation(blockIndex, avatarSheets.WorldSheet,
                GameConfig.IsEditor, name);
            var questListVar = new QuestList(
                avatarSheets.QuestSheet,
                avatarSheets.QuestRewardSheet,
                avatarSheets.QuestItemRewardSheet,
                avatarSheets.EquipmentItemRecipeSheet,
                avatarSheets.EquipmentItemSubRecipeSheet
            );
            return new AvatarState(
                address, agentAddress, blockIndex, questListVar, worldInformationVar, rankingMapAddress, name);
        }

        public AvatarState(Address address,
            Address agentAddress,
            long blockIndex,
            QuestList questList,
            WorldInformation worldInformation,
            Address rankingMapAddress,
            string name = null) : base(address)
        {
            Version = CurrentVersion;
            this.name = name ?? string.Empty;
            characterId = GameConfig.DefaultAvatarCharacterId;
            level = 1;
            exp = 0;
            inventory = new Inventory();
            this.worldInformation = worldInformation;
            updatedAt = blockIndex;
            this.agentAddress = agentAddress;
            this.questList = questList;
            mailBox = new MailBox();
            this.blockIndex = blockIndex;
            stageMap = new CollectionMap();
            monsterMap = new CollectionMap();
            itemMap = new CollectionMap();
            const QuestEventType createEvent = QuestEventType.Create;
            const QuestEventType levelEvent = QuestEventType.Level;
            eventMap = new CollectionMap
            {
                new KeyValuePair<int, int>((int) createEvent, 1),
                new KeyValuePair<int, int>((int) levelEvent, level),
            };

            combinationSlotAddresses = new List<Address>();
            RankingMapAddress = rankingMapAddress;
            UpdateGeneralQuest(new[] { createEvent, levelEvent });
            UpdateCompletedQuest();

            PostConstructor();
        }

        public AvatarState(AvatarState avatarState) : base(avatarState.address)
        {
            if (avatarState == null)
                throw new ArgumentNullException(nameof(avatarState));

            Version = avatarState.Version;
            name = avatarState.name;
            characterId = avatarState.characterId;
            level = avatarState.level;
            exp = avatarState.exp;
            inventory = avatarState.inventory;
            worldInformation = avatarState.worldInformation;
            updatedAt = avatarState.updatedAt;
            agentAddress = avatarState.agentAddress;
            questList = avatarState.questList;
            mailBox = avatarState.mailBox;
            blockIndex = avatarState.blockIndex;
            dailyRewardReceivedIndex = avatarState.dailyRewardReceivedIndex;
            actionPoint = avatarState.actionPoint;
            stageMap = avatarState.stageMap;
            monsterMap = avatarState.monsterMap;
            itemMap = avatarState.itemMap;
            eventMap = avatarState.eventMap;
            hair = avatarState.hair;
            lens = avatarState.lens;
            ear = avatarState.ear;
            tail = avatarState.tail;
            combinationSlotAddresses = avatarState.combinationSlotAddresses;
            RankingMapAddress = avatarState.RankingMapAddress;

            PostConstructor();
        }

        public AvatarState(Dictionary serialized)
            : base(serialized)
        {
            Version = 1;
            string nameKey = NameKey;
            string characterIdKey = CharacterIdKey;
            string levelKey = LevelKey;
            string expKey = ExpKey;
            string inventoryKey = LegacyInventoryKey;
            string worldInformationKey = LegacyWorldInformationKey;
            string updatedAtKey = UpdatedAtKey;
            string agentAddressKey = AgentAddressKey;
            string questListKey = LegacyQuestListKey;
            string mailBoxKey = MailBoxKey;
            string blockIndexKey = BlockIndexKey;
            string dailyRewardReceivedIndexKey = DailyRewardReceivedIndexKey;
            string actionPointKey = ActionPointKey;
            string stageMapKey = StageMapKey;
            string monsterMapKey = MonsterMapKey;
            string itemMapKey = ItemMapKey;
            string eventMapKey = EventMapKey;
            string hairKey = HairKey;
            string lensKey = LensKey;
            string earKey = EarKey;
            string tailKey = TailKey;
            string combinationSlotAddressesKey = CombinationSlotAddressesKey;
            string rankingMapAddressKey = RankingMapAddressKey;
            if (serialized.ContainsKey(LegacyNameKey))
            {
                nameKey = LegacyNameKey;
                characterIdKey = LegacyCharacterIdKey;
                levelKey = LegacyLevelKey;
                updatedAtKey = LegacyUpdatedAtKey;
                agentAddressKey = LegacyAgentAddressKey;
                mailBoxKey = LegacyMailBoxKey;
                blockIndexKey = LegacyBlockIndexKey;
                dailyRewardReceivedIndexKey = LegacyDailyRewardReceivedIndexKey;
                actionPointKey = LegacyActionPointKey;
                stageMapKey = LegacyStageMapKey;
                monsterMapKey = LegacyMonsterMapKey;
                itemMapKey = LegacyItemMapKey;
                eventMapKey = LegacyEventMapKey;
                hairKey = LegacyHairKey;
                earKey = LegacyEarKey;
                tailKey = LegacyTailKey;
                combinationSlotAddressesKey = LegacyCombinationSlotAddressesKey;
                rankingMapAddressKey = LegacyRankingMapAddressKey;
            }

            name = serialized[nameKey].ToDotnetString();
            characterId = (int)((Integer)serialized[characterIdKey]).Value;
            level = (int)((Integer)serialized[levelKey]).Value;
            exp = (long)((Integer)serialized[expKey]).Value;
            updatedAt = serialized[updatedAtKey].ToLong();
            agentAddress = serialized[agentAddressKey].ToAddress();
            mailBox = new MailBox((List)serialized[mailBoxKey]);
            blockIndex = (long)((Integer)serialized[blockIndexKey]).Value;
            dailyRewardReceivedIndex = (long)((Integer)serialized[dailyRewardReceivedIndexKey]).Value;
            actionPoint = (int)((Integer)serialized[actionPointKey]).Value;
            stageMap = new CollectionMap((Dictionary)serialized[stageMapKey]);
            serialized.TryGetValue((Text)monsterMapKey, out var value2);
            monsterMap = value2 is null ? new CollectionMap() : new CollectionMap((Dictionary)value2);
            itemMap = new CollectionMap((Dictionary)serialized[itemMapKey]);
            eventMap = new CollectionMap((Dictionary)serialized[eventMapKey]);
            hair = (int)((Integer)serialized[hairKey]).Value;
            lens = (int)((Integer)serialized[lensKey]).Value;
            ear = (int)((Integer)serialized[earKey]).Value;
            tail = (int)((Integer)serialized[tailKey]).Value;
            combinationSlotAddresses = serialized[combinationSlotAddressesKey].ToList(StateExtensions.ToAddress);
            RankingMapAddress = serialized[rankingMapAddressKey].ToAddress();

            if (serialized.ContainsKey(inventoryKey))
            {
                Version = 0;
                inventory = new Inventory((List)serialized[inventoryKey]);
            }

            if (serialized.ContainsKey(worldInformationKey))
            {
                Version = 0;
                worldInformation = new WorldInformation((Dictionary)serialized[worldInformationKey]);
            }

            if (serialized.ContainsKey(questListKey))
            {
                Version = 0;
                questList = new QuestList((Dictionary)serialized[questListKey]);
            }

            PostConstructor();
        }

        public AvatarState(List serialized) : base(serialized[0])
        {
            Version = (int)((Integer)serialized[1]).Value;
            name = serialized[2].ToDotnetString();
            characterId = (int)((Integer)serialized[3]).Value;
            level = (int)((Integer)serialized[4]).Value;
            exp = (long)((Integer)serialized[5]).Value;
            updatedAt = serialized[6].ToLong();
            agentAddress = serialized[7].ToAddress();
            mailBox = new MailBox((List)serialized[8]);
            blockIndex = (long)((Integer)serialized[9]).Value;
            dailyRewardReceivedIndex = (long)((Integer)serialized[10]).Value;
            actionPoint = (int)((Integer)serialized[11]).Value;
            stageMap = new CollectionMap((Dictionary)serialized[12]);
            monsterMap = new CollectionMap((Dictionary)serialized[13]);
            itemMap = new CollectionMap((Dictionary)serialized[14]);
            eventMap = new CollectionMap((Dictionary)serialized[15]);
            hair = (int)((Integer)serialized[16]).Value;
            lens = (int)((Integer)serialized[17]).Value;
            ear = (int)((Integer)serialized[18]).Value;
            tail = (int)((Integer)serialized[19]).Value;
            combinationSlotAddresses = serialized[20].ToList(StateExtensions.ToAddress);
            RankingMapAddress = serialized[21].ToAddress();

            PostConstructor();
        }

        private void PostConstructor()
        {
            NameWithHash = $"{name} <size=80%><color=#A68F7E>#{address.ToHex().Substring(0, 4)}</color></size>";
        }

        public void Update(IStageSimulator stageSimulator)
        {
            var player = stageSimulator.Player;
            characterId = player.RowData.Id;
            level = player.Level;
            exp = player.Exp.Current;
            inventory = player.Inventory;
            worldInformation = player.worldInformation;
#pragma warning disable LAA1002
            foreach (var pair in player.monsterMap)
#pragma warning restore LAA1002
            {
                monsterMap.Add(pair);
            }

#pragma warning disable LAA1002
            foreach (var pair in player.eventMap)
#pragma warning restore LAA1002
            {
                eventMap.Add(pair);
            }

            if (stageSimulator.Log.IsClear)
            {
                stageMap.Add(new KeyValuePair<int, int>(stageSimulator.StageId, 1));
            }

#pragma warning disable LAA1002
            foreach (var pair in stageSimulator.ItemMap)
#pragma warning restore LAA1002
            {
                itemMap.Add(pair);
            }

            UpdateStageQuest(stageSimulator.Reward);
        }

        public void Apply(Player player, long index)
        {
            characterId = player.RowData.Id;
            level = player.Level;
            exp = player.Exp.Current;
            inventory = player.Inventory;
            updatedAt = index;
        }

        public object Clone()
        {
            var avatar = new AvatarState((List)SerializeList())
            {
                inventory = (Inventory)inventory.Clone(),
                worldInformation = (WorldInformation)worldInformation.Clone(),
                questList = (QuestList)questList.Clone()
            };
            return avatar;
        }

        public void Update(Mail.Mail mail)
        {
            mailBox.Add(mail);
            mailBox.CleanUp();
        }

        [Obsolete("Use Update")]
        public void Update2(Mail.Mail mail)
        {
            mailBox.Add(mail);
        }

        [Obsolete("Use Update")]
        public void Update3(Mail.Mail mail)
        {
            mailBox.Add(mail);
            mailBox.CleanUpV1();
        }

        [Obsolete("No longer in use.")]
        public void UpdateTemp(Mail.Mail mail, long currentBlockIndex)
        {
            mailBox.Add(mail);
            mailBox.CleanUpTemp(currentBlockIndex);
        }

        public void Customize(int hair, int lens, int ear, int tail)
        {
            this.hair = hair;
            this.lens = lens;
            this.ear = ear;
            this.tail = tail;
        }

        public void UpdateGeneralQuest(IEnumerable<QuestEventType> types)
        {
            eventMap = questList.UpdateGeneralQuest(types, eventMap);
        }

        private void UpdateCompletedQuest()
        {
            eventMap = questList.UpdateCompletedQuest(eventMap);
        }

        private void UpdateStageQuest(IEnumerable<ItemBase> items)
        {
            questList.UpdateStageQuest(stageMap);
            questList.UpdateMonsterQuest(monsterMap);
            questList.UpdateCollectQuest(itemMap);
            questList.UpdateItemTypeCollectQuest(items);
            UpdateGeneralQuest(new[] { QuestEventType.Level, QuestEventType.Die });
            UpdateCompletedQuest();
        }

        public void UpdateFromRapidCombination(CombinationConsumable5.ResultModel result,
            long requiredIndex)
        {
            var mail = mailBox.First(m => m.id == result.id);
            mail.requiredBlockIndex = requiredIndex;
            var item = inventory.Items
                .Select(i => i.item)
                .OfType<ItemUsable>()
                .First(i => i.ItemId == result.itemUsable.ItemId);
            item.Update(requiredIndex);
        }

        public void UpdateFromRapidCombinationV2(RapidCombination5.ResultModel result,
            long requiredIndex)
        {
            var mail = mailBox.First(m => m.id == result.id);
            mail.requiredBlockIndex = requiredIndex;
            var item = inventory.Items
                .Select(i => i.item)
                .OfType<ItemUsable>()
                .First(i => i.ItemId == result.itemUsable.ItemId);
            item.Update(requiredIndex);
        }

        // todo 1: 퀘스트 전용 함수임을 알 수 있는 네이밍이 필요함.
        // todo 2: 혹은 분리된 객체에게 위임하면 좋겠음.
        #region Quest From Action

        public void UpdateFromCombination(ItemUsable itemUsable)
        {
            questList.UpdateCombinationQuest(itemUsable);
            var type = itemUsable.ItemType == ItemType.Equipment ? QuestEventType.Equipment : QuestEventType.Consumable;
            eventMap.Add(new KeyValuePair<int, int>((int)type, 1));
            UpdateGeneralQuest(new[] { type });
            UpdateCompletedQuest();
            UpdateFromAddItem(itemUsable, false);
        }

        public void UpdateFromItemEnhancement(Equipment equipment)
        {
            questList.UpdateItemEnhancementQuest(equipment);
            var type = QuestEventType.Enhancement;
            eventMap.Add(new KeyValuePair<int, int>((int)type, 1));
            UpdateGeneralQuest(new[] { type });
            UpdateCompletedQuest();
            UpdateFromAddItem(equipment, false);
        }

        public void UpdateFromAddItem(ItemUsable itemUsable, bool canceled)
        {
            var pair = inventory.AddItem(itemUsable);
            itemMap.Add(pair);

            if (!canceled)
            {
                questList.UpdateCollectQuest(itemMap);
                questList.UpdateItemGradeQuest(itemUsable);
                questList.UpdateItemTypeCollectQuest(new[] { itemUsable, });
            }

            UpdateCompletedQuest();
        }

        public void UpdateFromAddItem(ItemBase itemUsable, int count, bool canceled)
        {
            var pair = inventory.AddItem(itemUsable, count: count);
            itemMap.Add(pair);

            if (!canceled)
            {
                questList.UpdateCollectQuest(itemMap);
                questList.UpdateItemTypeCollectQuest(new[] { itemUsable });
            }

            UpdateCompletedQuest();
        }

        public void UpdateFromAddCostume(Costume costume)
        {
            var pair = inventory.AddItem(costume);
            itemMap.Add(pair);
        }

        public void UpdateFromQuestReward(Quest.Quest quest, MaterialItemSheet materialItemSheet)
        {
            var items = new List<Material>();
            foreach (var pair in quest.Reward.ItemMap.OrderBy(kv => kv.Item1))
            {
                var row = materialItemSheet.OrderedList.First(itemRow => itemRow.Id == pair.Item1);
                var item = ItemFactory.CreateMaterial(row);
                var map = inventory.AddItem(item, count: pair.Item2);
                itemMap.Add(map);
                items.Add(item);
            }

            quest.IsPaidInAction = true;
            questList.UpdateCollectQuest(itemMap);
            questList.UpdateItemTypeCollectQuest(items);
            UpdateCompletedQuest();
        }

        public void UpdateQuestRewards(MaterialItemSheet materialItemSheet)
        {
            var completedQuests = questList
                .Where(quest => quest.Complete && !quest.IsPaidInAction)
                .ToList();
            // 완료되었지만 보상을 받지 않은 퀘스트를 return 문에서 Select 하지 않고 미리 저장하는 이유는
            // 지연된 실행에 의해, return 시점에서 이미 모든 퀘스트의 보상 처리가 완료된 상태에서
            // completed를 호출 시 where문의 predicate가 평가되어 컬렉션이 텅 비기 때문이다.
            var completedQuestIds = completedQuests.Select(quest => quest.Id).ToList();
            foreach (var quest in completedQuests)
            {
                UpdateFromQuestReward(quest, materialItemSheet);
            }

            questList.completedQuestIds = completedQuestIds;
        }

        #endregion

        public int GetArmorId()
        {
            var armor = inventory.Items.Select(i => i.item).OfType<Armor>().FirstOrDefault(e => e.equipped);
            return armor?.Id ?? GameConfig.DefaultAvatarArmorId;
        }

        public int GetPortraitId()
        {
            var fc = inventory.Costumes.FirstOrDefault(e => e.ItemSubType == ItemSubType.FullCostume);
            return fc?.Id ?? GetArmorId();
        }

        public void ValidateEquipments(List<Guid> equipmentIds, long blockIndex)
        {
            var ringCount = 0;
            foreach (var itemId in equipmentIds)
            {
                if (!inventory.TryGetNonFungibleItem(itemId, out ItemUsable outNonFungibleItem))
                {
                    throw new ItemDoesNotExistException($"Equipment {itemId} does not exist.");
                }

                var equipment = (Equipment) outNonFungibleItem;
                if (equipment.RequiredBlockIndex > blockIndex)
                {
                    throw new RequiredBlockIndexException($"{equipment.ItemSubType} / unlock on {equipment.RequiredBlockIndex}");
                }

                var requiredLevel = 0;
                switch (equipment.ItemSubType)
                {
                    case ItemSubType.Weapon:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterEquipmentSlotWeapon;
                        break;
                    case ItemSubType.Armor:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterEquipmentSlotArmor;
                        break;
                    case ItemSubType.Belt:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterEquipmentSlotBelt;
                        break;
                    case ItemSubType.Necklace:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterEquipmentSlotNecklace;
                        break;
                    case ItemSubType.Ring:
                        ringCount++;
                        requiredLevel = ringCount == 1
                            ? GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing1
                            : ringCount == 2
                                ? GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing2
                                : int.MaxValue;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"{equipment.ItemSubType} / invalid equipment type");
                }

                if (level < requiredLevel)
                {
                    throw new EquipmentSlotUnlockException($"{equipment.ItemSubType} / not enough level. required: {requiredLevel}");
                }
            }
        }

        public List<Equipment> ValidateEquipmentsV2(List<Guid> equipmentIds, long blockIndex)
        {
            var countMap = new Dictionary<ItemSubType, int>();
            var list = new List<Equipment>();
            foreach (var itemId in equipmentIds)
            {
                if (!inventory.TryGetNonFungibleItem(itemId, out ItemUsable outNonFungibleItem))
                {
                    throw new ItemDoesNotExistException($"Equipment {itemId} does not exist.");
                }

                var equipment = (Equipment)outNonFungibleItem;
                if (equipment.RequiredBlockIndex > blockIndex)
                {
                    throw new RequiredBlockIndexException($"{equipment.ItemSubType} / unlock on {equipment.RequiredBlockIndex}");
                }

                var type = equipment.ItemSubType;
                if (!countMap.ContainsKey(type))
                {
                    countMap[type] = 0;
                }

                countMap[type] += 1;

                var requiredLevel = 0;
                var isSlotEnough = true;
                switch (equipment.ItemSubType)
                {
                    case ItemSubType.Weapon:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Weapon;
                        requiredLevel = isSlotEnough ?
                            GameConfig.RequireCharacterLevel.CharacterEquipmentSlotWeapon : int.MaxValue;
                        break;
                    case ItemSubType.Armor:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Armor;
                        requiredLevel = isSlotEnough ?
                            GameConfig.RequireCharacterLevel.CharacterEquipmentSlotArmor : int.MaxValue;
                        break;
                    case ItemSubType.Belt:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Belt;
                        requiredLevel = isSlotEnough ?
                            GameConfig.RequireCharacterLevel.CharacterEquipmentSlotBelt : int.MaxValue;
                        break;
                    case ItemSubType.Necklace:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Necklace;
                        requiredLevel = isSlotEnough ?
                            GameConfig.RequireCharacterLevel.CharacterEquipmentSlotNecklace : int.MaxValue;
                        break;
                    case ItemSubType.Ring:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Ring;
                        requiredLevel = countMap[ItemSubType.Ring] == 1
                            ? GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing1
                            : countMap[ItemSubType.Ring] == 2
                                ? GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing2
                                : int.MaxValue;
                        break;
                    case ItemSubType.Aura:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Aura;
                        requiredLevel = isSlotEnough ?
                            GameConfig.RequireCharacterLevel.CharacterEquipmentSlotAura : int.MaxValue;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"{equipment.ItemSubType} / invalid equipment type");
                }

                if (!isSlotEnough)
                {
                    throw new DuplicateEquipmentException($"Equipment slot of {equipment.ItemSubType} is full, but tried to equip {equipment.Id}");
                }

                if (level < requiredLevel)
                {
                    throw new EquipmentSlotUnlockException($"{equipment.ItemSubType} / not enough level. required: {requiredLevel}");
                }

                list.Add(equipment);
            }

            return list;
        }

        public List<Equipment> ValidateEquipmentsV3(List<Guid> equipmentIds, long blockIndex, GameConfigState gameConfigState)
        {
            var countMap = new Dictionary<ItemSubType, int>();
            var list = new List<Equipment>();
            foreach (var itemId in equipmentIds)
            {
                if (!inventory.TryGetNonFungibleItem(itemId, out ItemUsable outNonFungibleItem))
                {
                    throw new ItemDoesNotExistException($"Equipment {itemId} does not exist.");
                }

                var equipment = (Equipment)outNonFungibleItem;
                if (equipment.RequiredBlockIndex > blockIndex)
                {
                    throw new RequiredBlockIndexException($"{equipment.ItemSubType} / unlock on {equipment.RequiredBlockIndex}");
                }

                var type = equipment.ItemSubType;
                if (!countMap.ContainsKey(type))
                {
                    countMap[type] = 0;
                }

                countMap[type] += 1;

                var requiredLevel = 0;
                var isSlotEnough = true;
                switch (equipment.ItemSubType)
                {
                    case ItemSubType.Weapon:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Weapon;
                        requiredLevel = isSlotEnough ?
                            gameConfigState.RequireCharacterLevel_EquipmentSlotWeapon : int.MaxValue;
                        break;
                    case ItemSubType.Armor:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Armor;
                        requiredLevel = isSlotEnough ?
                            gameConfigState.RequireCharacterLevel_EquipmentSlotArmor : int.MaxValue;
                        break;
                    case ItemSubType.Belt:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Belt;
                        requiredLevel = isSlotEnough ?
                            gameConfigState.RequireCharacterLevel_EquipmentSlotBelt : int.MaxValue;
                        break;
                    case ItemSubType.Necklace:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Necklace;
                        requiredLevel = isSlotEnough ?
                            gameConfigState.RequireCharacterLevel_EquipmentSlotNecklace : int.MaxValue;
                        break;
                    case ItemSubType.Ring:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Ring;
                        requiredLevel = countMap[ItemSubType.Ring] == 1
                            ? gameConfigState.RequireCharacterLevel_EquipmentSlotRing1
                            : countMap[ItemSubType.Ring] == 2
                                ? gameConfigState.RequireCharacterLevel_EquipmentSlotRing2
                                : int.MaxValue;
                        break;
                    case ItemSubType.Aura:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Aura;
                        requiredLevel = isSlotEnough ?
                            gameConfigState.RequireCharacterLevel_EquipmentSlotAura : int.MaxValue;
                        break;
                    case ItemSubType.Grimoire:
                        isSlotEnough = countMap[type] <= GameConfig.MaxEquipmentSlotCount.Grimoire;
                        requiredLevel = isSlotEnough ?
                            gameConfigState.RequireCharacterLevel_EquipmentSlotGrimoire : int.MaxValue;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException($"{equipment.ItemSubType} / invalid equipment type");
                }

                if (!isSlotEnough)
                {
                    throw new DuplicateEquipmentException($"Equipment slot of {equipment.ItemSubType} is full, but tried to equip {equipment.Id}");
                }

                if (level < requiredLevel)
                {
                    throw new EquipmentSlotUnlockException($"{equipment.ItemSubType} / not enough level. required: {requiredLevel}");
                }

                list.Add(equipment);
            }

            return list;
        }

        public List<int> ValidateConsumable(List<Guid> consumableIds, long currentBlockIndex)
        {
            var list = new List<int>();
            for (var slotIndex = 0; slotIndex < consumableIds.Count; slotIndex++)
            {
                var consumableId = consumableIds[slotIndex];

                if (!inventory.TryGetNonFungibleItem(consumableId, out ItemUsable outNonFungibleItem))
                {
                    continue;
                }

                var equipment = (Consumable) outNonFungibleItem;
                if (equipment.RequiredBlockIndex > currentBlockIndex)
                {
                    throw new RequiredBlockIndexException(
                        $"{equipment.ItemSubType} / unlock on {equipment.RequiredBlockIndex}");
                }

                int requiredLevel;
                switch (slotIndex)
                {
                    case 0:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterConsumableSlot1;
                        break;
                    case 1:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterConsumableSlot2;
                        break;
                    case 2:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterConsumableSlot3;
                        break;
                    case 3:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterConsumableSlot4;
                        break;
                    case 4:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterConsumableSlot5;
                        break;
                    default:
                        throw new ConsumableSlotOutOfRangeException();
                }

                if (level < requiredLevel)
                {
                    throw new ConsumableSlotUnlockException($"not enough level. required: {requiredLevel}");
                }

                list.Add(equipment.Id);
            }

            return list;
        }

        public List<int> ValidateConsumableV2(
            List<Guid> consumableIds,
            long currentBlockIndex,
            GameConfigState gameConfigState)
        {
            var list = new List<int>();
            for (var slotIndex = 0; slotIndex < consumableIds.Count; slotIndex++)
            {
                var consumableId = consumableIds[slotIndex];

                if (!inventory.TryGetNonFungibleItem(consumableId, out ItemUsable outNonFungibleItem))
                {
                    continue;
                }

                var equipment = (Consumable) outNonFungibleItem;
                if (equipment.RequiredBlockIndex > currentBlockIndex)
                {
                    throw new RequiredBlockIndexException(
                        $"{equipment.ItemSubType} / unlock on {equipment.RequiredBlockIndex}");
                }

                int requiredLevel;
                switch (slotIndex)
                {
                    case 0:
                        requiredLevel = gameConfigState.RequireCharacterLevel_ConsumableSlot1;
                        break;
                    case 1:
                        requiredLevel = gameConfigState.RequireCharacterLevel_ConsumableSlot2;
                        break;
                    case 2:
                        requiredLevel = gameConfigState.RequireCharacterLevel_ConsumableSlot3;
                        break;
                    case 3:
                        requiredLevel = gameConfigState.RequireCharacterLevel_ConsumableSlot4;
                        break;
                    case 4:
                        requiredLevel = gameConfigState.RequireCharacterLevel_ConsumableSlot5;
                        break;
                    default:
                        throw new ConsumableSlotOutOfRangeException();
                }

                if (level < requiredLevel)
                {
                    throw new ConsumableSlotUnlockException($"not enough level. required: {requiredLevel}");
                }

                list.Add(equipment.Id);
            }

            return list;
        }

        public List<int> ValidateCostume(IEnumerable<Guid> costumeIds)
        {
            var subTypes = new List<ItemSubType>();
            var list = new List<int>();
            foreach (var costumeId in costumeIds)
            {
                if (!inventory.TryGetNonFungibleItem<Costume>(costumeId, out var costume))
                {
                    continue;
                }

                if (subTypes.Contains(costume.ItemSubType))
                {
                    throw new DuplicateCostumeException($"can't equip duplicate costume type : {costume.ItemSubType}");
                }

                subTypes.Add(costume.ItemSubType);

                int requiredLevel;
                switch (costume.ItemSubType)
                {
                    case ItemSubType.FullCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot;
                        break;
                    case ItemSubType.HairCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterHairCostumeSlot;
                        break;
                    case ItemSubType.EarCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterEarCostumeSlot;
                        break;
                    case ItemSubType.EyeCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterEyeCostumeSlot;
                        break;
                    case ItemSubType.TailCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterTailCostumeSlot;
                        break;
                    case ItemSubType.Title:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterTitleSlot;
                        break;
                    default:
                        throw new InvalidItemTypeException(
                            $"Costume[id: {costumeId}] isn't expected type. [type: {costume.ItemSubType}]");
                }

                if (level < requiredLevel)
                {
                    throw new CostumeSlotUnlockException($"not enough level. required: {requiredLevel}");
                }

                list.Add(costume.Id);
            }

            return list;
        }

        public List<Costume> ValidateCostumeV2(IEnumerable<Guid> costumeIds, GameConfigState gameConfigState)
        {
            var subTypes = new List<ItemSubType>();
            var list = new List<Costume>();
            foreach (var costumeId in costumeIds)
            {
                if (!inventory.TryGetNonFungibleItem<Costume>(costumeId, out var costume))
                {
                    continue;
                }

                if (subTypes.Contains(costume.ItemSubType))
                {
                    throw new DuplicateCostumeException($"can't equip duplicate costume type : {costume.ItemSubType}");
                }

                subTypes.Add(costume.ItemSubType);

                int requiredLevel;
                switch (costume.ItemSubType)
                {
                    case ItemSubType.FullCostume:
                        requiredLevel = gameConfigState.RequireCharacterLevel_FullCostumeSlot;
                        break;
                    case ItemSubType.HairCostume:
                        requiredLevel = gameConfigState.RequireCharacterLevel_HairCostumeSlot;
                        break;
                    case ItemSubType.EarCostume:
                        requiredLevel = gameConfigState.RequireCharacterLevel_EarCostumeSlot;
                        break;
                    case ItemSubType.EyeCostume:
                        requiredLevel = gameConfigState.RequireCharacterLevel_EyeCostumeSlot;
                        break;
                    case ItemSubType.TailCostume:
                        requiredLevel = gameConfigState.RequireCharacterLevel_TailCostumeSlot;
                        break;
                    case ItemSubType.Title:
                        requiredLevel = gameConfigState.RequireCharacterLevel_TitleSlot;
                        break;
                    default:
                        throw new InvalidItemTypeException(
                            $"Costume[id: {costumeId}] isn't expected type. [type: {costume.ItemSubType}]");
                }

                if (level < requiredLevel)
                {
                    throw new CostumeSlotUnlockException($"not enough level. required: {requiredLevel}");
                }

                list.Add(costume);
            }

            return list;
        }

        public void ValidateCostume(HashSet<int> costumeIds)
        {
            var subTypes = new List<ItemSubType>();
            foreach (var costumeId in costumeIds.OrderBy(i => i))
            {
#pragma warning disable 618
                if (!inventory.TryGetCostume(costumeId, out var costume))
#pragma warning restore 618
                {
                    continue;
                }

                if (subTypes.Contains(costume.ItemSubType))
                {
                    throw new DuplicateCostumeException($"can't equip duplicate costume type : {costume.ItemSubType}");
                }
                subTypes.Add(costume.ItemSubType);

                int requiredLevel;
                switch (costume.ItemSubType)
                {
                    case ItemSubType.FullCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot;
                        break;
                    case ItemSubType.HairCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterHairCostumeSlot;
                        break;
                    case ItemSubType.EarCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterEarCostumeSlot;
                        break;
                    case ItemSubType.EyeCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterEyeCostumeSlot;
                        break;
                    case ItemSubType.TailCostume:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterTailCostumeSlot;
                        break;
                    case ItemSubType.Title:
                        requiredLevel = GameConfig.RequireCharacterLevel.CharacterTitleSlot;
                        break;
                    default:
                        throw new InvalidItemTypeException(
                            $"Costume[id: {costumeId}] isn't expected type. [type: {costume.ItemSubType}]");
                }

                if (level < requiredLevel)
                {
                    throw new CostumeSlotUnlockException($"not enough level. required: {requiredLevel}");
                }
            }
        }

        public void ValidateItemRequirement(
            List<int> itemIds,
            List<Equipment> equipments,
            ItemRequirementSheet requirementSheet,
            EquipmentItemRecipeSheet recipeSheet,
            EquipmentItemSubRecipeSheetV2 subRecipeSheet,
            EquipmentItemOptionSheet itemOptionSheet,
            string addressesHex)
        {
            foreach (var id in itemIds)
            {
                if (!requirementSheet.TryGetValue(id, out var requirementRow))
                {
                    throw new SheetRowNotFoundException(addressesHex, nameof(ItemRequirementSheet), id);
                }

                if (level < requirementRow.Level)
                {
                    throw new NotEnoughAvatarLevelException(id, false, requirementRow.Level, level);
                }
            }

            foreach (var equipment in equipments)
            {
                if (!requirementSheet.TryGetValue(equipment.Id, out var requirementRow))
                {
                    throw new SheetRowNotFoundException(addressesHex, nameof(ItemRequirementSheet), equipment.Id);
                }

                var isMadeWithMimisbrunnrRecipe = equipment.IsMadeWithMimisbrunnrRecipe(
                    recipeSheet,
                    subRecipeSheet,
                    itemOptionSheet
                );
                var requirementLevel = isMadeWithMimisbrunnrRecipe
                    ? requirementRow.MimisLevel
                    : requirementRow.Level;
                if (level < requirementLevel)
                {
                    throw new NotEnoughAvatarLevelException(equipment.Id, isMadeWithMimisbrunnrRecipe, requirementLevel, level);
                }
            }
        }

        public void EquipItems(IEnumerable<Guid> itemIds)
        {
            // Unequip items already equipped.
            var equippableItems = inventory.Items
                .Select(item => item.item)
                .OfType<IEquippableItem>()
                .Where(equippableItem => equippableItem.Equipped);
#pragma warning disable LAA1002
            foreach (var equippableItem in equippableItems)
#pragma warning restore LAA1002
            {
                equippableItem.Unequip();
            }

            // Equip items.
            foreach (var itemId in itemIds)
            {
                if (!inventory.TryGetNonFungibleItem(itemId, out var inventoryItem) ||
                    !(inventoryItem.item is IEquippableItem equippableItem))
                {
                    continue;
                }

                equippableItem.Equip();
            }
        }

        // FIXME: Use `EquipItems(IEnumerable<Guid>)` instead of this.
        public void EquipCostumes(HashSet<int> costumeIds)
        {
            // 코스튬 해제.
            var inventoryCostumes = inventory.Items
                .Select(i => i.item)
                .OfType<Costume>()
                .Where(i => i.equipped)
                .ToImmutableHashSet();
#pragma warning disable LAA1002
            foreach (var costume in inventoryCostumes)
#pragma warning restore LAA1002
            {
                // FIXME: Use `costume.Unequip()`
                costume.equipped = false;
            }

            // 코스튬 장착.
            foreach (var costumeId in costumeIds.OrderBy(i => i))
            {
#pragma warning disable 618
                if (!inventory.TryGetCostume(costumeId, out var costume))
#pragma warning restore 618
                {
                    continue;
                }

                // FIXME: Use `costume.Unequip()`
                costume.equipped = true;
            }
        }

        // FIXME: Use `EquipItems(IEnumerable<Guid>)` instead of this.
        public void EquipEquipments(List<Guid> equipmentIds)
        {
            // 장비 해제.
            var inventoryEquipments = inventory.Items
                .Select(i => i.item)
                .OfType<Equipment>()
                .Where(i => i.equipped)
                .ToImmutableHashSet();
#pragma warning disable LAA1002
            foreach (var equipment in inventoryEquipments)
#pragma warning restore LAA1002
            {
                equipment.Unequip();
            }

            // 장비 장착.
            foreach (var equipmentId in equipmentIds)
            {
                if (!inventory.TryGetNonFungibleItem(equipmentId, out ItemUsable outNonFungibleItem))
                {
                    continue;
                }

                ((Equipment) outNonFungibleItem).Equip();
            }
        }

        public int GetRandomSeed()
        {
            var bytes = address.ToByteArray().Concat(BitConverter.GetBytes(Nonce)).ToArray();
            var hash = SHA256.Create().ComputeHash(bytes);
            Nonce++;
            return BitConverter.ToInt32(hash, 0);
        }

        public List<T> GetNonFungibleItems<T>(List<Guid> itemIds)
        {
            var items = new List<T>();
            foreach (var nonFungibleId in itemIds)
            {
                if (!inventory.TryGetNonFungibleItem(nonFungibleId, out var inventoryItem))
                {
                    continue;
                }

                if (inventoryItem.item is T item)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        /// <inheritdoc cref="IState.Serialize" />
        public override IValue Serialize()
        {
            return SerializeList();
        }

        public IValue SerializeList()
        {
            // Migrated when serialized
            Version = CurrentVersion;
            return new List(
                base.SerializeListBase(),
                (Integer)Version,
                (Text)name,
                (Integer)characterId,
                (Integer)level,
                (Integer)exp,
                updatedAt.Serialize(),
                agentAddress.Serialize(),
                mailBox.Serialize(),
                (Integer)blockIndex,
                (Integer)dailyRewardReceivedIndex,
                (Integer)actionPoint,
                stageMap.Serialize(),
                monsterMap.Serialize(),
                itemMap.Serialize(),
                eventMap.Serialize(),
                (Integer)hair,
                (Integer)lens,
                (Integer)ear,
                (Integer)tail,
                combinationSlotAddresses
                    .OrderBy(i => i)
                    .Select(i => i.Serialize())
                    .Serialize(),
                RankingMapAddress.Serialize()
            );
        }
    }
}
