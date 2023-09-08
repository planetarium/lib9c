namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Arena;
    using Nekoyume.Model;
    using Nekoyume.Model.BattleStatus.Arena;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class AuraScenarioTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Address _enemyAvatarAddress;
        private readonly IWorld _initialState;
        private readonly Aura _aura;
        private readonly TableSheets _tableSheets;

        public AuraScenarioTest()
        {
            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);
            _avatarAddress = new PrivateKey().ToAddress();
            _enemyAvatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = _avatarAddress.Derive("ranking_map");
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            var auraRow =
                _tableSheets.EquipmentItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Aura);
            _aura = (Aura)ItemFactory.CreateItemUsable(auraRow, Guid.NewGuid(), 0L);
            _aura.StatsMap.AddStatAdditionalValue(StatType.CRI, 1);
            var skillRow = _tableSheets.SkillSheet[800001];
            var skill = SkillFactory.Get(skillRow, 0, 100, 0, StatType.NONE);
            _aura.Skills.Add(skill);
            var addresses = new[] { _avatarAddress, _enemyAvatarAddress };
            _initialState = new MockWorld();
            for (int i = 0; i < addresses.Length; i++)
            {
                var avatarAddress = addresses[i];
                agentState.avatarAddresses.Add(i, avatarAddress);
                var avatarState = new AvatarState(
                    _avatarAddress,
                    _agentAddress,
                    0,
                    _tableSheets.GetAvatarSheets(),
                    gameConfigState,
                    rankingMapAddress
                );
                avatarState.inventory.AddItem(_aura);
                _initialState = AvatarModule.SetAvatarState(
                    _initialState,
                    avatarAddress,
                    avatarState,
                    true,
                    true,
                    true,
                    true);
            }

            _initialState = AgentModule.SetAgentState(_initialState, _agentAddress, agentState);
            _initialState = LegacyModule.SetState(
                _initialState,
                Addresses.GoldCurrency,
                new GoldCurrencyState(Currency.Legacy("NCG", 2, minters: null)).Serialize());
            _initialState = LegacyModule.SetState(
                _initialState,
                gameConfigState.address,
                gameConfigState.Serialize());
            _initialState = LegacyModule.MintAsset(
                _initialState,
                new ActionContext(),
                _agentAddress,
                Currencies.Crystal * 2);
            foreach (var (key, value) in sheets)
            {
                _initialState = LegacyModule.SetState(
                    _initialState,
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }
        }

        [Fact]
        public void HackAndSlash()
        {
            var itemSlotStateAddress = ItemSlotState.DeriveAddress(_avatarAddress, BattleType.Adventure);
            Assert.Null(LegacyModule.GetState(_initialState, itemSlotStateAddress));

            var has = new HackAndSlash
            {
                StageId = 1,
                AvatarAddress = _avatarAddress,
                Equipments = new List<Guid>
                {
                    _aura.ItemId,
                },
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                WorldId = 1,
                RuneInfos = new List<RuneSlotInfo>(),
            };

            var nextState = has.Execute(new ActionContext
            {
                BlockIndex = 2,
                PreviousState = _initialState,
                Random = new TestRandom(),
                Signer = _agentAddress,
            });

            var avatarState = AvatarModule.GetAvatarState(_initialState, _avatarAddress);
            Assert_Player(avatarState, nextState, _avatarAddress, itemSlotStateAddress);
        }

        [Fact]
        public void Raid()
        {
            var itemSlotStateAddress = ItemSlotState.DeriveAddress(_avatarAddress, BattleType.Raid);
            Assert.Null(LegacyModule.GetState(_initialState, itemSlotStateAddress));
            var avatarState = AvatarModule.GetAvatarState(_initialState, _avatarAddress);
            for (int i = 0; i < 50; i++)
            {
                avatarState.worldInformation.ClearStage(1, i + 1, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
            }

            var prevState = AvatarModule.SetAvatarState(
                _initialState,
                _avatarAddress,
                avatarState,
                false,
                false,
                true,
                false
            );

            var raid = new Raid
            {
                AvatarAddress = _avatarAddress,
                EquipmentIds = new List<Guid>
                {
                    _aura.ItemId,
                },
                CostumeIds = new List<Guid>(),
                FoodIds = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
            };

            var nextState = raid.Execute(new ActionContext
            {
                BlockIndex = 5045201,
                PreviousState = prevState,
                Random = new TestRandom(),
                Signer = _agentAddress,
            });
            Assert_Player(avatarState, nextState, _avatarAddress, itemSlotStateAddress);
        }

        [Fact]
        public void Arena()
        {
            var prevState = _initialState;
            var addresses = new[] { _avatarAddress, _enemyAvatarAddress };
            foreach (var avatarAddress in addresses)
            {
                var itemSlotStateAddress = ItemSlotState.DeriveAddress(avatarAddress, BattleType.Arena);
                Assert.Null(LegacyModule.GetState(_initialState, itemSlotStateAddress));

                var avatarState = AvatarModule.GetAvatarState(prevState, avatarAddress);
                for (int i = 0; i < 50; i++)
                {
                    avatarState.worldInformation.ClearStage(1, i + 1, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
                }

                prevState = AvatarModule.SetAvatarState(
                    prevState,
                    avatarAddress,
                    avatarState,
                    false,
                    false,
                    true,
                    false
                );

                var join = new JoinArena
                {
                    avatarAddress = avatarAddress,
                    championshipId = 1,
                    round = 1,
                    costumes = new List<Guid>(),
                    equipments = new List<Guid>
                    {
                        _aura.ItemId,
                    },
                    runeInfos = new List<RuneSlotInfo>(),
                };
                var nextState = join.Execute(new ActionContext
                {
                    BlockIndex = 1,
                    Signer = _agentAddress,
                    PreviousState = prevState,
                });
                var arenaAvatarStateAdr = ArenaAvatarState.DeriveAddress(avatarAddress);
                var serializedArenaAvatarState = (List)LegacyModule.GetState(nextState, arenaAvatarStateAdr);
                var arenaAvatarState = new ArenaAvatarState(serializedArenaAvatarState);
                Assert_Equipments(arenaAvatarState.Equipments);
                prevState = nextState;
            }

            foreach (var avatarAddress in addresses)
            {
                var enemyAvatarAddress = avatarAddress.Equals(_avatarAddress)
                    ? _enemyAvatarAddress
                    : _avatarAddress;
                var battle = new BattleArena
                {
                    myAvatarAddress = avatarAddress,
                    enemyAvatarAddress = enemyAvatarAddress,
                    championshipId = 1,
                    round = 1,
                    ticket = 1,
                    costumes = new List<Guid>(),
                    equipments = new List<Guid>
                    {
                        _aura.ItemId,
                    },
                    runeInfos = new List<RuneSlotInfo>(),
                };

                var nextState = battle.Execute(new ActionContext
                {
                    Signer = _agentAddress,
                    PreviousState = prevState,
                    BlockIndex = 2,
                    Random = new TestRandom(),
                });
                var avatarState = AvatarModule.GetAvatarState(prevState, avatarAddress);
                var enemyAvatarState = AvatarModule.GetAvatarState(prevState, enemyAvatarAddress);
                var simulator = new ArenaSimulator(new TestRandom());
                var myArenaPlayerDigest = new ArenaPlayerDigest(
                    avatarState,
                    battle.equipments,
                    battle.costumes,
                    new List<RuneState>()
                );
                var enemySlotAddress =
                    ItemSlotState.DeriveAddress(enemyAvatarAddress, BattleType.Arena);
                var enemySlotState = Assert_ItemSlot(prevState, enemySlotAddress);
                var enemyArenaPlayerDigest = new ArenaPlayerDigest(
                    enemyAvatarState,
                    enemySlotState.Equipments,
                    enemySlotState.Costumes,
                    new List<RuneState>()
                );
                var log = simulator.Simulate(
                    myArenaPlayerDigest,
                    enemyArenaPlayerDigest,
                    _tableSheets.GetArenaSimulatorSheets());
                // Check player, enemy equip aura
                foreach (var spawn in log.OfType<ArenaSpawnCharacter>())
                {
                    ArenaCharacter character = spawn.Character;
                    Assert.Equal(21, character.ATK);
                    Assert.Equal(11, character.CRI);
                }

                Assert_ItemSlot(nextState, ItemSlotState.DeriveAddress(avatarAddress, BattleType.Arena));
                prevState = nextState;
            }
        }

        private void Assert_Player(AvatarState avatarState, IWorld world, Address avatarAddress, Address itemSlotStateAddress)
        {
            var nextAvatarState = AvatarModule.GetAvatarState(world, avatarAddress);
            var equippedItem = Assert.IsType<Aura>(nextAvatarState.inventory.Equipments.First());
            Assert.True(equippedItem.equipped);
            Assert_ItemSlot(world, itemSlotStateAddress);
            var player = new Player(avatarState, _tableSheets.GetSimulatorSheets());
            var equippedPlayer = new Player(nextAvatarState, _tableSheets.GetSimulatorSheets());
            Assert.Null(player.aura);
            Assert.NotNull(equippedPlayer.aura);
            Assert.Equal(player.ATK + 1, equippedPlayer.ATK);
            Assert.Equal(player.CRI + 1, equippedPlayer.CRI);
        }

        private void Assert_Equipments(IEnumerable<Guid> equipments)
        {
            var equipmentId = Assert.Single(equipments);
            Assert.Equal(_aura.ItemId, equipmentId);
        }

        private ItemSlotState Assert_ItemSlot(IWorld world, Address itemSlotStateAddress)
        {
            var rawItemSlot =
                Assert.IsType<List>(LegacyModule.GetState(world, itemSlotStateAddress));
            var itemSlotState = new ItemSlotState(rawItemSlot);
            Assert_Equipments(itemSlotState.Equipments);
            return itemSlotState;
        }
    }
}
