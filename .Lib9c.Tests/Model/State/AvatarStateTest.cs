namespace Lib9c.Tests.Model.State
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using Bencodex;
    using Bencodex.Types;
    using Lib9c.Tests.Model.Item;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.Quest;
    using Nekoyume.Model.State;
    using Xunit;

    public class AvatarStateTest
    {
        private readonly TableSheets _tableSheets;

        public AvatarStateTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
        }

        [Fact]
        public void Serialize()
        {
            var avatarAddress = new PrivateKey().Address;
            var agentAddress = new PrivateKey().Address;
            var avatarState = GetNewAvatarState(avatarAddress, agentAddress);

            var serialized = avatarState.SerializeList();
            var deserialized = new AvatarState((Bencodex.Types.List)serialized);

            Assert.Equal(avatarState.address, deserialized.address);
            Assert.Equal(avatarState.agentAddress, deserialized.agentAddress);
            Assert.Equal(avatarState.blockIndex, deserialized.blockIndex);
            Assert.Null(deserialized.inventory);
            Assert.Null(deserialized.worldInformation);
            Assert.Null(deserialized.questList);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        public async Task ConstructDeterministic(int waitMilliseconds)
        {
            var avatarAddress = new PrivateKey().Address;
            var agentAddress = new PrivateKey().Address;
            var avatarStateA = GetNewAvatarState(avatarAddress, agentAddress);
            await Task.Delay(waitMilliseconds);
            var avatarStateB = GetNewAvatarState(avatarAddress, agentAddress);

            HashDigest<SHA256> Hash(AvatarState avatarState)
            {
                return HashDigest<SHA256>.DeriveFrom(new Codec().Encode(avatarState.SerializeList()));
            }

            Assert.Equal(Hash(avatarStateA), Hash(avatarStateB));
        }

        [Fact]
        public void UpdateFromQuestRewardDeterministic()
        {
            var rankingState = new RankingState1();
            var avatarAddress = new PrivateKey().Address;
            var agentAddress = new PrivateKey().Address;
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                rankingState.UpdateRankingMap(avatarAddress));
            var itemIds = avatarState.questList.OfType<ItemTypeCollectQuest>().First().ItemIds;
            var map = new Dictionary<int, int>()
            {
                [400000] = 1,
                [302002] = 1,
                [302003] = 1,
                [302001] = 1,
                [306023] = 1,
                [302000] = 1,
            };

            var serialized = (Dictionary)avatarState.questList.OfType<WorldQuest>().First().Serialize();
            serialized = serialized.SetItem("reward", new Nekoyume.Model.Quest.QuestReward(map).Serialize());

            var quest = new WorldQuest(serialized);

            avatarState.UpdateFromQuestReward(quest, _tableSheets.MaterialItemSheet);
            Assert.Equal(
                avatarState.questList.OfType<ItemTypeCollectQuest>().First().ItemIds,
                new List<int>()
                {
                    302000,
                    302001,
                    302002,
                    302003,
                    306023,
                }
            );
        }

        [Theory]
        [InlineData(1, GameConfig.RequireCharacterLevel.CharacterConsumableSlot1)]
        [InlineData(2, GameConfig.RequireCharacterLevel.CharacterConsumableSlot2)]
        [InlineData(3, GameConfig.RequireCharacterLevel.CharacterConsumableSlot3)]
        [InlineData(4, GameConfig.RequireCharacterLevel.CharacterConsumableSlot4)]
        [InlineData(5, GameConfig.RequireCharacterLevel.CharacterConsumableSlot5)]
        public void ValidateConsumable(int count, int level)
        {
            var avatarAddress = new PrivateKey().Address;
            var agentAddress = new PrivateKey().Address;
            var avatarState = GetNewAvatarState(avatarAddress, agentAddress);
            avatarState.level = level;

            var consumableIds = new List<Guid>();
            var row = _tableSheets.ConsumableItemSheet.Values.First();
            for (var i = 0; i < count; i++)
            {
                var id = Guid.NewGuid();
                var consumable = ItemFactory.CreateItemUsable(row, id, 0);
                consumableIds.Add(id);
                avatarState.inventory.AddItem(consumable);
            }

            avatarState.ValidateConsumable(consumableIds, 0);
        }

        [Fact]
        public void ValidateConsumableThrowRequiredBlockIndexException()
        {
            var avatarAddress = new PrivateKey().Address;
            var agentAddress = new PrivateKey().Address;
            var avatarState = GetNewAvatarState(avatarAddress, agentAddress);

            var consumableIds = new List<Guid>();
            var row = _tableSheets.ConsumableItemSheet.Values.First();
            var id = Guid.NewGuid();
            var consumable = ItemFactory.CreateItemUsable(row, id, 1);
            consumableIds.Add(id);
            avatarState.inventory.AddItem(consumable);
            Assert.Throws<RequiredBlockIndexException>(() => avatarState.ValidateConsumable(consumableIds, 0));
        }

        [Fact]
        public void ValidateConsumableThrowConsumableSlotOutOfRangeException()
        {
            var avatarAddress = new PrivateKey().Address;
            var agentAddress = new PrivateKey().Address;
            var avatarState = GetNewAvatarState(avatarAddress, agentAddress);
            avatarState.level = GameConfig.RequireCharacterLevel.CharacterConsumableSlot5;

            var consumableIds = new List<Guid>();
            var row = _tableSheets.ConsumableItemSheet.Values.First();
            for (var i = 0; i < 6; i++)
            {
                var id = Guid.NewGuid();
                var consumable = ItemFactory.CreateItemUsable(row, id, 0);
                consumableIds.Add(id);
                avatarState.inventory.AddItem(consumable);
            }

            Assert.Throws<ConsumableSlotOutOfRangeException>(() => avatarState.ValidateConsumable(consumableIds, 0));
        }

        [Theory]
        [InlineData(1, GameConfig.RequireCharacterLevel.CharacterConsumableSlot1)]
        [InlineData(2, GameConfig.RequireCharacterLevel.CharacterConsumableSlot2)]
        [InlineData(3, GameConfig.RequireCharacterLevel.CharacterConsumableSlot3)]
        [InlineData(4, GameConfig.RequireCharacterLevel.CharacterConsumableSlot4)]
        [InlineData(5, GameConfig.RequireCharacterLevel.CharacterConsumableSlot5)]
        public void ValidateConsumableSlotThrowConsumableSlotUnlockException(int count, int level)
        {
            var avatarAddress = new PrivateKey().Address;
            var agentAddress = new PrivateKey().Address;
            var avatarState = GetNewAvatarState(avatarAddress, agentAddress);
            avatarState.level = level - 1;

            var consumableIds = new List<Guid>();
            var row = _tableSheets.ConsumableItemSheet.Values.First();
            for (var i = 0; i < count; i++)
            {
                var id = Guid.NewGuid();
                var consumable = ItemFactory.CreateItemUsable(row, id, 0);
                consumableIds.Add(id);
                avatarState.inventory.AddItem(consumable);
            }

            Assert.Throws<ConsumableSlotUnlockException>(() => avatarState.ValidateConsumable(consumableIds, 0));
        }

        [Fact]
        public void ValidateCostume()
        {
            var avatarAddress = new PrivateKey().Address;
            var agentAddress = new PrivateKey().Address;
            var avatarState = GetNewAvatarState(avatarAddress, agentAddress);
            avatarState.level = 100;

            var costumeIds = new HashSet<int>();
            var subTypes = new[]
            {
                ItemSubType.FullCostume,
                ItemSubType.HairCostume,
                ItemSubType.EarCostume,
                ItemSubType.EyeCostume,
                ItemSubType.TailCostume,
                ItemSubType.Title,
            };
            foreach (var subType in subTypes)
            {
                var row = _tableSheets.CostumeItemSheet.Values.First(r => r.ItemSubType == subType);
                var costume = ItemFactory.CreateCostume(row, default);
                costumeIds.Add(costume.Id);
                avatarState.inventory.AddItem(costume);
            }

            avatarState.ValidateCostume(costumeIds);
        }

        [Theory]
        [InlineData(ItemSubType.FullCostume)]
        [InlineData(ItemSubType.HairCostume)]
        [InlineData(ItemSubType.EarCostume)]
        [InlineData(ItemSubType.EyeCostume)]
        [InlineData(ItemSubType.TailCostume)]
        [InlineData(ItemSubType.Title)]
        public void ValidateCostumeThrowDuplicateCostumeException(ItemSubType type)
        {
            var avatarAddress = new PrivateKey().Address;
            var agentAddress = new PrivateKey().Address;
            var avatarState = GetNewAvatarState(avatarAddress, agentAddress);
            avatarState.level = 100;

            var costumeIds = new HashSet<int>();
            var duplicateRows = _tableSheets.CostumeItemSheet.Values.Where(r => r.ItemSubType == type);
            var row = _tableSheets.CostumeItemSheet.Values.First(r => r.ItemSubType != type);
            var costume = ItemFactory.CreateCostume(row, default);
            costumeIds.Add(costume.Id);
            avatarState.inventory.AddItem(costume);

            foreach (var duplicateRow in duplicateRows)
            {
                var duplicateCostume = ItemFactory.CreateCostume(duplicateRow, default);
                costumeIds.Add(duplicateCostume.Id);
                avatarState.inventory.AddItem(duplicateCostume);
            }

            Assert.Throws<DuplicateCostumeException>(() => avatarState.ValidateCostume(costumeIds));
        }

        [Theory]
        [InlineData(ItemSubType.FullCostume, GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot)]
        [InlineData(ItemSubType.HairCostume, GameConfig.RequireCharacterLevel.CharacterHairCostumeSlot)]
        [InlineData(ItemSubType.EarCostume, GameConfig.RequireCharacterLevel.CharacterEarCostumeSlot)]
        [InlineData(ItemSubType.EyeCostume, GameConfig.RequireCharacterLevel.CharacterEyeCostumeSlot)]
        [InlineData(ItemSubType.TailCostume, GameConfig.RequireCharacterLevel.CharacterTailCostumeSlot)]
        [InlineData(ItemSubType.Title, GameConfig.RequireCharacterLevel.CharacterTitleSlot)]
        public void ValidateCostumeThrowCostumeSlotUnlockException(ItemSubType type, int level)
        {
            var avatarAddress = new PrivateKey().Address;
            var agentAddress = new PrivateKey().Address;
            var avatarState = GetNewAvatarState(avatarAddress, agentAddress);
            avatarState.level = level - 1;

            var row = _tableSheets.CostumeItemSheet.Values.First(r => r.ItemSubType == type);
            var costume = ItemFactory.CreateCostume(row, default);
            var costumeIds = new HashSet<int> { costume.Id, };
            avatarState.inventory.AddItem(costume);

            Assert.Throws<CostumeSlotUnlockException>(() => avatarState.ValidateCostume(costumeIds));
        }

        [Fact]
        public void UpdateV2()
        {
            var avatarAddress = new PrivateKey().Address;
            var agentAddress = new PrivateKey().Address;
            var avatarState = GetNewAvatarState(avatarAddress, agentAddress);
            var result = new CombinationConsumable5.ResultModel()
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = null,
            };
            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                avatarState.Update(mail);
            }

            Assert.Equal(30, avatarState.mailBox.Count);
            Assert.DoesNotContain(avatarState.mailBox, m => m.blockIndex < 30);
        }

        [Fact]
        public void UpdateV4()
        {
            var avatarAddress = new PrivateKey().Address;
            var agentAddress = new PrivateKey().Address;
            var avatarState = GetNewAvatarState(avatarAddress, agentAddress);
            var result = new CombinationConsumable5.ResultModel()
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = null,
            };
            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, i);
                avatarState.Update(mail);
            }

            Assert.Equal(30, avatarState.mailBox.Count);

            var newMail = new CombinationMail(result, 101, default, 101);
            avatarState.UpdateTemp(newMail, 101);
            Assert.Single(avatarState.mailBox);
        }

        [Fact]
        public void CleanUpMail()
        {
            var result = new CombinationConsumable5.ResultModel()
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = null,
            };
            var mailBox = new MailBox();
            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                mailBox.Add(mail);
            }

            mailBox.CleanUpV1();

            Assert.Equal(30, mailBox.Count);
            Assert.DoesNotContain(mailBox, m => m.blockIndex < 30);
        }

        [Fact]
        public void EquipItems()
        {
            var avatarState = GetNewAvatarState(new PrivateKey().Address, new PrivateKey().Address);
            avatarState.inventory.AddItem(EquipmentTest.CreateFirstEquipment(_tableSheets));
            avatarState.inventory.AddItem(CostumeTest.CreateFirstCostume(_tableSheets));

            var equippableItems = avatarState.inventory.Items
                .Select(item => item.item)
                .OfType<IEquippableItem>()
                .ToImmutableHashSet();
            foreach (var equippableItem in equippableItems)
            {
                Assert.False(equippableItem.Equipped);
            }

            avatarState.EquipItems(
                equippableItems.OfType<INonFungibleItem>()
                    .Select(nonFungibleItem => nonFungibleItem.NonFungibleId));

            foreach (var equippableItem in equippableItems)
            {
                Assert.True(equippableItem.Equipped);
            }
        }

        [Fact]
        public void Clone()
        {
            var avatarState = GetNewAvatarState(default, default);
            var clonedAvatar = (AvatarState)avatarState.Clone();
            Assert.Equal(clonedAvatar.SerializeList(), avatarState.SerializeList());
            var weaponRows = _tableSheets
                .EquipmentItemSheet
                .Values
                .Where(r => r.ItemSubType == ItemSubType.Weapon)
                .Take(1);
            foreach (var row in weaponRows)
            {
                var equipment = ItemFactory.CreateItemUsable(
                        _tableSheets.EquipmentItemSheet[row.Id],
                        Guid.NewGuid(),
                        0)
                    as Equipment;

                clonedAvatar.inventory.AddItem(equipment);
            }

            // Make sure the inventory is cloned
            Assert.NotEqual(avatarState.inventory.Serialize(), clonedAvatar.inventory.Serialize());
        }

        [Theory]
        [InlineData(ItemSubType.Weapon, 1, GameConfig.MaxEquipmentSlotCount.Weapon, 0, 0)]
        [InlineData(ItemSubType.Armor, 1, GameConfig.MaxEquipmentSlotCount.Armor, 0, 1)]
        [InlineData(ItemSubType.Belt, 2, GameConfig.MaxEquipmentSlotCount.Belt, 0, 0)]
        [InlineData(ItemSubType.Necklace, 1, GameConfig.MaxEquipmentSlotCount.Necklace, 0, 1)]
        [InlineData(ItemSubType.Ring, 3, GameConfig.MaxEquipmentSlotCount.Ring, 0, 0)]
        private void ValidateEquipmentsV2(ItemSubType type, int count, int maxCount, long blockIndex, long requiredBlockIndex)
        {
            var avatarState = GetNewAvatarState(new PrivateKey().Address, new PrivateKey().Address);
            var maxLevel = _tableSheets.CharacterLevelSheet.Max(row => row.Value.Level);
            var expRow = _tableSheets.CharacterLevelSheet[maxLevel];
            var maxLevelExp = expRow.Exp;

            avatarState.level = maxLevel;
            avatarState.exp = maxLevelExp;

            var weaponRows = _tableSheets
                .EquipmentItemSheet
                .Values
                .Where(r => r.ItemSubType == type)
                .Take(count);

            var equipments = new List<Guid>();
            foreach (var row in weaponRows)
            {
                var equipment = ItemFactory.CreateItemUsable(
                        _tableSheets.EquipmentItemSheet[row.Id],
                        Guid.NewGuid(),
                        requiredBlockIndex)
                    as Equipment;

                equipments.Add(equipment.ItemId);
                avatarState.inventory.AddItem(equipment);
            }

            try
            {
                avatarState.ValidateEquipmentsV2(equipments, blockIndex);
            }
            catch (Exception e)
            {
                if (blockIndex < requiredBlockIndex)
                {
                    Assert.True(e is RequiredBlockIndexException);
                }
                else if (count > maxCount)
                {
                    Assert.True(e is DuplicateEquipmentException);
                }
            }
        }

        private AvatarState GetNewAvatarState(Address avatarAddress, Address agentAddress)
        {
            var rankingState = new RankingState1();
            return AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                rankingState.UpdateRankingMap(avatarAddress));
        }
    }
}
