namespace Lib9c.Tests.Module
{
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Action;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.Quest;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class AvatarModuleTest
    {
        private readonly Address _address;
        private readonly string _name;
        private readonly int _characterId;
        private readonly int _level;
        private readonly int _exp;
        private readonly Inventory _inventory;
        private readonly WorldInformation _worldInformation;
        private readonly long _updatedAt;
        private readonly Address _agentAddress;
        private readonly QuestList _questList;
        private readonly MailBox _mailBox;
        private readonly long _blockIndex;
        private readonly long _dailyRewardReceivedIndex;
        private readonly int _actionPoint;
        private readonly CollectionMap _stageMap;
        private readonly CollectionMap _monsterMap;
        private readonly CollectionMap _itemMap;
        private readonly CollectionMap _eventMap;
        private readonly int _hair;
        private readonly int _lens;
        private readonly int _ear;
        private readonly int _tail;
        private readonly List<Address> _combinationSlotAddresses;
        private readonly Address _rankingMapAddress;

        public AvatarModuleTest()
        {
            _address = new PrivateKey().ToAddress();
            _name = "foo";
            _characterId = 1;
            _level = 100;
            _exp = 1000;
            _inventory = new Inventory();
            var (tableSheets, _, _, _) = InitializeUtil.InitializeStates();
            var row = tableSheets.EquipmentItemSheet.First;
            var itemUsable = ItemFactory.CreateItem(row, new TestRandom());
            _inventory.AddItem(itemUsable);
            var worldSheet = tableSheets.WorldSheet;
            _worldInformation = new WorldInformation(0L, worldSheet, 0);
            _updatedAt = 1234L;
            _agentAddress = new PrivateKey().ToAddress();

            // FIXME: Should fill QuestList.
            _questList = new QuestList(
                new QuestSheet(),
                new QuestRewardSheet(),
                new QuestItemRewardSheet(),
                new EquipmentItemRecipeSheet(),
                new EquipmentItemSubRecipeSheet()
            );

            // FIXME: Should fill MailBox.
            _mailBox = new MailBox();

            _blockIndex = 703120;
            _dailyRewardReceivedIndex = 703110;
            _actionPoint = 70;

            _stageMap = new CollectionMap { new KeyValuePair<int, int>(1, 1) };
            _monsterMap = new CollectionMap { new KeyValuePair<int, int>(0, 10) };
            _itemMap = new CollectionMap { new KeyValuePair<int, int>(7, 1) };
            _eventMap = new CollectionMap { new KeyValuePair<int, int>(2, 1) };

            _hair = 1;
            _lens = 2;
            _ear = 3;
            _tail = 4;

            _combinationSlotAddresses = new List<Address>
            {
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
            };

            _rankingMapAddress = new PrivateKey().ToAddress();
        }

        [Fact]
        public void GetAvatarStateV0()
        {
            var dict = new Dictionary<IKey, IValue>
            {
                [(Text)LegacyAddressKey] = _address.Serialize(),
                [(Text)LegacyNameKey] = (Text)_name,
                [(Text)LegacyCharacterIdKey] = (Integer)_characterId,
                [(Text)LegacyLevelKey] = (Integer)_level,
                [(Text)ExpKey] = (Integer)_exp,
                [(Text)LegacyInventoryKey] = _inventory.Serialize(),
                [(Text)LegacyWorldInformationKey] = _worldInformation.Serialize(),
                [(Text)LegacyUpdatedAtKey] = _updatedAt.Serialize(),
                [(Text)LegacyAgentAddressKey] = _agentAddress.Serialize(),
                [(Text)LegacyQuestListKey] = _questList.Serialize(),
                [(Text)LegacyMailBoxKey] = _mailBox.Serialize(),
                [(Text)LegacyBlockIndexKey] = (Integer)_blockIndex,
                [(Text)LegacyDailyRewardReceivedIndexKey] = (Integer)_dailyRewardReceivedIndex,
                [(Text)LegacyActionPointKey] = (Integer)_actionPoint,
                [(Text)LegacyStageMapKey] = _stageMap.Serialize(),
                [(Text)LegacyMonsterMapKey] = _monsterMap.Serialize(),
                [(Text)LegacyItemMapKey] = _itemMap.Serialize(),
                [(Text)LegacyEventMapKey] = _eventMap.Serialize(),
                [(Text)LegacyHairKey] = (Integer)_hair,
                [(Text)LensKey] = (Integer)_lens,
                [(Text)LegacyEarKey] = (Integer)_ear,
                [(Text)LegacyTailKey] = (Integer)_tail,
                [(Text)LegacyCombinationSlotAddressesKey] = _combinationSlotAddresses
                    .OrderBy(i => i)
                    .Select(i => i.Serialize())
                    .Serialize(),
                [(Text)LegacyRankingMapAddressKey] = _rankingMapAddress.Serialize(),
            };

            IWorld world = new MockWorld();
            IAccount account = new MockAccount(ReservedAddresses.LegacyAccount);
            account = account.SetState(_address, new Dictionary(dict));
            world = world.SetAccount(account);
            var avatarStateV0 = AvatarModule.GetAvatarState(world, _address);
            CheckAvatarState(avatarStateV0, 0);
        }

        [Fact]
        public void GetAvatarStateV1()
        {
            var dict = new Dictionary<IKey, IValue>
            {
                [(Text)AddressKey] = _address.Serialize(),
                [(Text)NameKey] = (Text)_name,
                [(Text)CharacterIdKey] = (Integer)_characterId,
                [(Text)LevelKey] = (Integer)_level,
                [(Text)ExpKey] = (Integer)_exp,
                [(Text)UpdatedAtKey] = _updatedAt.Serialize(),
                [(Text)AgentAddressKey] = _agentAddress.Serialize(),
                [(Text)MailBoxKey] = _mailBox.Serialize(),
                [(Text)BlockIndexKey] = (Integer)_blockIndex,
                [(Text)DailyRewardReceivedIndexKey] = (Integer)_dailyRewardReceivedIndex,
                [(Text)ActionPointKey] = (Integer)_actionPoint,
                [(Text)StageMapKey] = _stageMap.Serialize(),
                [(Text)MonsterMapKey] = _monsterMap.Serialize(),
                [(Text)ItemMapKey] = _itemMap.Serialize(),
                [(Text)EventMapKey] = _eventMap.Serialize(),
                [(Text)HairKey] = (Integer)_hair,
                [(Text)LensKey] = (Integer)_lens,
                [(Text)EarKey] = (Integer)_ear,
                [(Text)TailKey] = (Integer)_tail,
                [(Text)CombinationSlotAddressesKey] = _combinationSlotAddresses
                    .OrderBy(i => i)
                    .Select(i => i.Serialize())
                    .Serialize(),
                [(Text)RankingMapAddressKey] = _rankingMapAddress.Serialize(),
            };

            IWorld world = new MockWorld();
            IAccount account = new MockAccount(ReservedAddresses.LegacyAccount);
            account = account.SetState(_address, new Dictionary(dict));
            account = account.SetState(_address.Derive(LegacyInventoryKey), _inventory.Serialize());
            account = account.SetState(_address.Derive(LegacyWorldInformationKey), _worldInformation.Serialize());
            account = account.SetState(_address.Derive(LegacyQuestListKey), _questList.Serialize());
            world = world.SetAccount(account);
            var avatarStateV1 = AvatarModule.GetAvatarState(world, _address);
            CheckAvatarState(avatarStateV1, 1);
        }

        [Fact]
        public void GetAvatarStateV2()
        {
            int version = 2;
            var list = new List<IValue>
            {
                new List(_address.Serialize()),
                (Integer)version,
                (Text)_name,
                (Integer)_characterId,
                (Integer)_level,
                (Integer)_exp,
                _updatedAt.Serialize(),
                _agentAddress.Serialize(),
                _mailBox.Serialize(),
                (Integer)_blockIndex,
                (Integer)_dailyRewardReceivedIndex,
                (Integer)_actionPoint,
                _stageMap.Serialize(),
                _monsterMap.Serialize(),
                _itemMap.Serialize(),
                _eventMap.Serialize(),
                (Integer)_hair,
                (Integer)_lens,
                (Integer)_ear,
                (Integer)_tail,
                _combinationSlotAddresses
                    .OrderBy(i => i)
                    .Select(i => i.Serialize())
                    .Serialize(),
                _rankingMapAddress.Serialize(),
            };

            IWorld world = new MockWorld();
            IAccount avatarAccount = world.GetAccount(Addresses.Avatar).SetState(_address, new List(list));
            IAccount inventoryAccount = world.GetAccount(Addresses.Inventory).SetState(_address, _inventory.Serialize());
            IAccount worldInformationAccount = world.GetAccount(Addresses.WorldInformation).SetState(_address, _worldInformation.Serialize());
            IAccount questListAccount = world.GetAccount(Addresses.QuestList).SetState(_address, _questList.Serialize());
            world = world.SetAccount(avatarAccount);
            world = world.SetAccount(inventoryAccount);
            world = world.SetAccount(worldInformationAccount);
            world = world.SetAccount(questListAccount);
            var avatarStateV2 = AvatarModule.GetAvatarState(world, _address);
            CheckAvatarState(avatarStateV2, version);
        }

        private void CheckAvatarState(AvatarState state, int expectedVersion)
        {
            Assert.NotNull(state);
            Assert.Equal(expectedVersion, state.Version);
            Assert.Equal(_address, state.address);
            Assert.Equal(_name, state.name);
            Assert.Equal(_characterId, state.characterId);
            Assert.Equal(_level, state.level);
            Assert.Equal(_exp, state.exp);
            Assert.Equal(_inventory, state.inventory);
            // Assert.Equal(_worldInformation, state.worldInformation);
            Assert.Equal(_updatedAt, state.updatedAt);
            Assert.Equal(_agentAddress, state.agentAddress);
            Assert.Equal(_questList, state.questList);
            Assert.Equal(_mailBox, state.mailBox);
            Assert.Equal(_blockIndex, state.blockIndex);
            Assert.Equal(_dailyRewardReceivedIndex, state.dailyRewardReceivedIndex);
            Assert.Equal(_actionPoint, state.actionPoint);
            Assert.Equal(_stageMap, state.stageMap);
            Assert.Equal(_monsterMap, state.monsterMap);
            Assert.Equal(_itemMap, state.itemMap);
            Assert.Equal(_eventMap, state.eventMap);
            Assert.Equal(_hair, state.hair);
            Assert.Equal(_lens, state.lens);
            Assert.Equal(_ear, state.ear);
            Assert.Equal(_tail, state.tail);
            Assert.Equal(
                _combinationSlotAddresses.ToHashSet(),
                state.combinationSlotAddresses.ToHashSet());
        }
    }
}
