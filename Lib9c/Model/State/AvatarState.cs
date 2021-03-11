using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.TableData;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// Agent가 포함하는 각 Avatar의 상태 모델이다.
    /// </summary>
    [Serializable]
    public class AvatarState : State, ICloneable
    {
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
        public List<Address> combinationSlotAddresses;
        public const int CombinationSlotCapacity = 4;

        public string NameWithHash { get; private set; }
        public int Nonce { get; private set; }

        public readonly Address RankingMapAddress;

        public static Address CreateAvatarAddress()
        {
            var key = new PrivateKey();
            return key.PublicKey.ToAddress();
        }

        public AvatarState(Address address,
            Address agentAddress,
            long blockIndex,
            AvatarSheets avatarSheets,
            GameConfigState gameConfigState,
            Address rankingMapAddress,
            string name = null) : base(address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            this.name = name ?? string.Empty;
            characterId = GameConfig.DefaultAvatarCharacterId;
            level = 1;
            exp = 0;
            inventory = new Inventory();
            worldInformation = new WorldInformation(blockIndex, avatarSheets.WorldSheet, GameConfig.IsEditor);
            updatedAt = blockIndex;
            this.agentAddress = agentAddress;
            questList = new QuestList(
                avatarSheets.QuestSheet,
                avatarSheets.QuestRewardSheet,
                avatarSheets.QuestItemRewardSheet,
                avatarSheets.EquipmentItemRecipeSheet,
                avatarSheets.EquipmentItemSubRecipeSheet
            );
            mailBox = new MailBox();
            this.blockIndex = blockIndex;
            actionPoint = gameConfigState.ActionPointMax;
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
            combinationSlotAddresses = new List<Address>(CombinationSlotCapacity);
            for (var i = 0; i < CombinationSlotCapacity; i++)
            {
                var slotAddress = address.Derive(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CombinationSlotState.DeriveFormat,
                        i
                    )
                );
                combinationSlotAddresses.Add(slotAddress);
            }

            combinationSlotAddresses = combinationSlotAddresses
                .OrderBy(element => element)
                .ToList();

            RankingMapAddress = rankingMapAddress;
            UpdateGeneralQuest(new[] { createEvent, levelEvent });
            UpdateCompletedQuest();

            PostConstructor();
        }

        public AvatarState(AvatarState avatarState) : base(avatarState.address)
        {
            if (avatarState == null)
                throw new ArgumentNullException(nameof(avatarState));

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
            name = ((Text)serialized["name"]).Value;
            characterId = (int)((Integer)serialized["characterId"]).Value;
            level = (int)((Integer)serialized["level"]).Value;
            exp = (long)((Integer)serialized["exp"]).Value;
            inventory = new Inventory((List)serialized["inventory"]);
            worldInformation = new WorldInformation((Dictionary)serialized["worldInformation"]);
            updatedAt = serialized["updatedAt"].ToLong();
            agentAddress = new Address(((Binary)serialized["agentAddress"]).Value);
            questList = new QuestList((Dictionary) serialized["questList"]);
            mailBox = new MailBox((List)serialized["mailBox"]);
            blockIndex = (long)((Integer)serialized["blockIndex"]).Value;
            dailyRewardReceivedIndex = (long)((Integer)serialized["dailyRewardReceivedIndex"]).Value;
            actionPoint = (int)((Integer)serialized["actionPoint"]).Value;
            stageMap = new CollectionMap((Dictionary)serialized["stageMap"]);
            serialized.TryGetValue((Text)"monsterMap", out var value2);
            monsterMap = value2 is null ? new CollectionMap() : new CollectionMap((Dictionary)value2);
            itemMap = new CollectionMap((Dictionary)serialized["itemMap"]);
            eventMap = new CollectionMap((Dictionary)serialized["eventMap"]);
            hair = (int)((Integer)serialized["hair"]).Value;
            lens = (int)((Integer)serialized["lens"]).Value;
            ear = (int)((Integer)serialized["ear"]).Value;
            tail = (int)((Integer)serialized["tail"]).Value;
            combinationSlotAddresses = serialized["combinationSlotAddresses"].ToList(StateExtensions.ToAddress);
            RankingMapAddress = serialized["ranking_map_address"].ToAddress();
            if (serialized.TryGetValue((Text) "nonce", out var nonceValue))
            {
                Nonce = nonceValue.ToInteger();
            }
            PostConstructor();
        }

        private void PostConstructor()
        {
            NameWithHash = $"{name} <size=80%><color=#A68F7E>#{address.ToHex().Substring(0, 4)}</color></size>";
        }

        public void Update(StageSimulator stageSimulator)
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

        public object Clone()
        {
            return MemberwiseClone();
        }

        public void Update(Mail.Mail mail)
        {
            mailBox.Add(mail);
        }

        public void UpdateV2(Mail.Mail mail)
        {
            mailBox.Add(mail);
            mailBox.CleanUp();
        }
        
        public void UpdateV3(Mail.Mail mail)
        {
            mailBox.Add(mail);
            mailBox.CleanUpV2();
        }

        public void UpdateV4(Mail.Mail mail, long currentBlockIndex)
        {
            mailBox.Add(mail);
            mailBox.CleanUpV3(currentBlockIndex);
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

        public void UpdateFromRapidCombination(CombinationConsumable.ResultModel result,
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
                questList.UpdateItemTypeCollectQuest(new[] { itemUsable });
            }

            UpdateCompletedQuest();
        }
        
        public void UpdateFromAddItem(ItemBase itemUsable, int count, bool canceled)
        {
            var pair = inventory.AddItem(itemUsable, count);
            itemMap.Add(pair);

            if (!canceled)
            {
                questList.UpdateCollectQuest(itemMap);
                questList.UpdateItemTypeCollectQuest(new[] { itemUsable });
            }

            UpdateCompletedQuest();
        }


        public void UpdateFromAddCostume(Costume costume, bool canceled)
        {
            var pair = inventory.AddItem(costume);
            itemMap.Add(pair);
        }

        public void UpdateFromQuestReward(Quest.Quest quest, MaterialItemSheet materialItemSheet)
        {
            var items = new List<Material>();
            foreach (var pair in quest.Reward.ItemMap.OrderBy(kv => kv.Key))
            {
                var row = materialItemSheet.OrderedList.First(itemRow => itemRow.Id == pair.Key);
                var item = ItemFactory.CreateMaterial(row);
                var map = inventory.AddItem(item, pair.Value);
                itemMap.Add(map);
                items.Add(item);
            }

            quest.IsPaidInAction = true;
            questList.UpdateCollectQuest(itemMap);
            questList.UpdateItemTypeCollectQuest(items);
            UpdateCompletedQuest();
        }

        /// <summary>
        /// 완료된 퀘스트의 보상 처리를 한다.
        /// </summary>
        public void UpdateQuestRewards(MaterialItemSheet materialItemSheet)
        {
            var completedQuests = questList.Where(quest => quest.Complete && !quest.IsPaidInAction);
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

        public void ValidateEquipments(List<Guid> equipmentIds, long blockIndex)
        {
            var ringCount = 0;
            foreach (var itemId in equipmentIds)
            {
                if (!inventory.TryGetNonFungibleItem(itemId, out ItemUsable outNonFungibleItem))
                {
                    continue;
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

        public void ValidateEquipmentsV2(List<Guid> equipmentIds, long blockIndex)
        {
            var countMap = new Dictionary<ItemSubType, int>();
            foreach (var itemId in equipmentIds)
            {
                if (!inventory.TryGetNonFungibleItem(itemId, out ItemUsable outNonFungibleItem))
                {
                    continue;
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
            }
        }

        public void ValidateConsumable(List<Guid> consumableIds, long currentBlockIndex)
        {
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
            }
        }
        
        public void ValidateCostume(IEnumerable<Guid> costumeIds)
        {
            var subTypes = new List<ItemSubType>();
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
            }
        }
        
        public void ValidateCostume(HashSet<int> costumeIds)
        {
            var subTypes = new List<ItemSubType>();
            foreach (var costumeId in costumeIds.OrderBy(i => i))
            {
                if (!inventory.TryGetCostume(costumeId, out var costume))
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

        public void EquipItems(IEnumerable<Guid> itemIds)
        {
            // Unequip items already equipped.
            var equippableItems = inventory.Items
                .Select(item => item.item)
                .OfType<IEquippableItem>()
                .Where(equippableItem => equippableItem.Equipped)
                .ToImmutableHashSet();
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
                if (!inventory.TryGetCostume(costumeId, out var costume))
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
            var hash = Hashcash.Hash(bytes);
            Nonce++;
            return BitConverter.ToInt32(hash.ToByteArray(), 0);
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"name"] = (Text)name,
                [(Text)"characterId"] = (Integer)characterId,
                [(Text)"level"] = (Integer)level,
                [(Text)"exp"] = (Integer)exp,
                [(Text)"inventory"] = inventory.Serialize(),
                [(Text)"worldInformation"] = worldInformation.Serialize(),
                [(Text)"updatedAt"] = updatedAt.Serialize(),
                [(Text)"agentAddress"] = agentAddress.Serialize(),
                [(Text)"questList"] = questList.Serialize(),
                [(Text)"mailBox"] = mailBox.Serialize(),
                [(Text)"blockIndex"] = (Integer)blockIndex,
                [(Text)"dailyRewardReceivedIndex"] = (Integer)dailyRewardReceivedIndex,
                [(Text)"actionPoint"] = (Integer)actionPoint,
                [(Text)"stageMap"] = stageMap.Serialize(),
                [(Text)"monsterMap"] = monsterMap.Serialize(),
                [(Text)"itemMap"] = itemMap.Serialize(),
                [(Text)"eventMap"] = eventMap.Serialize(),
                [(Text)"hair"] = (Integer)hair,
                [(Text)"lens"] = (Integer)lens,
                [(Text)"ear"] = (Integer)ear,
                [(Text)"tail"] = (Integer)tail,
                [(Text)"combinationSlotAddresses"] = combinationSlotAddresses
                    .OrderBy(i => i)
                    .Select(i => i.Serialize())
                    .Serialize(),
                [(Text) "nonce"] = Nonce.Serialize(),
                [(Text)"ranking_map_address"] = RankingMapAddress.Serialize(),
            }.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002
    }
}
