namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class CollectionScenarioTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Address _enemyAvatarAddress;
        private readonly IWorld _initialState;
        private readonly TableSheets _tableSheets;
        private readonly Dictionary<string, string> _sheets;

        public CollectionScenarioTest()
        {
            _agentAddress = new PrivateKey().Address;
            var agentState = new AgentState(_agentAddress);
            _avatarAddress = new PrivateKey().Address;
            _enemyAvatarAddress = new PrivateKey().Address;
            var rankingMapAddress = _avatarAddress.Derive("ranking_map");
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var addresses = new[] { _avatarAddress, _enemyAvatarAddress, };
            _initialState = new World(MockUtil.MockModernWorldState);
            for (var i = 0; i < addresses.Length; i++)
            {
                var avatarAddress = addresses[i];
                agentState.avatarAddresses.Add(i, avatarAddress);
                var avatarState = AvatarState.Create(
                    _avatarAddress,
                    _agentAddress,
                    0,
                    _tableSheets.GetAvatarSheets(),
                    rankingMapAddress
                );
                _initialState = _initialState.SetAvatarState(
                        avatarAddress,
                        avatarState,
                        true,
                        true,
                        true,
                        true)
                    .SetActionPoint(avatarAddress, DailyReward.ActionPointMax);
            }

            var currency = Currency.Legacy("NCG", 2, null);
            _initialState = _initialState
                .SetAgentState(_agentAddress, agentState)
                .SetLegacyState(
                    Addresses.GoldCurrency,
                    new GoldCurrencyState(currency).Serialize())
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize())
                .MintAsset(new ActionContext(), _agentAddress, Currencies.Crystal * 2);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HackAndSlash(bool collectionExist)
        {
            var states = _initialState;
            if (collectionExist)
            {
                var collectionState = new CollectionState();
                collectionState.Ids.Add(1);
                states = states.SetCollectionState(_avatarAddress, collectionState);
            }

            foreach (var (key, value) in _sheets)
            {
                if (key == nameof(CollectionSheet) && !collectionExist)
                {
                    continue;
                }

                states = states
                    .SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var action = new HackAndSlash
            {
                AvatarAddress = _avatarAddress,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                StageId = 1,
                WorldId = 1,
                RuneInfos = new List<RuneSlotInfo>(),
                Foods = new List<Guid>(),
            };

            action.Execute(
                new ActionContext
                {
                    PreviousState = states,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Raid(bool collectionExist)
        {
            var states = _initialState;
            if (collectionExist)
            {
                var collectionState = new CollectionState();
                collectionState.Ids.Add(1);
                states = states.SetCollectionState(_avatarAddress, collectionState);
            }

            foreach (var (key, value) in _sheets)
            {
                if (key == nameof(CollectionSheet) && !collectionExist)
                {
                    continue;
                }

                states = states.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var action = new Raid
            {
                AvatarAddress = _avatarAddress,
                EquipmentIds = new List<Guid>(),
                CostumeIds = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                FoodIds = new List<Guid>(),
            };
            action.Execute(
                new ActionContext
                {
                    PreviousState = states,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                    BlockIndex = _tableSheets.WorldBossListSheet.First().Value.StartedBlockIndex,
                });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BattleArena(bool collectionExist)
        {
            var prevStates = _initialState;
            if (collectionExist)
            {
                var collectionState = new CollectionState();
                collectionState.Ids.Add(1);
                prevStates = prevStates.SetCollectionState(_avatarAddress, collectionState);
            }

            foreach (var (key, value) in _sheets)
            {
                if (key == nameof(CollectionSheet) && !collectionExist)
                {
                    continue;
                }

                prevStates = prevStates.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var addresses = new[] { _avatarAddress, _enemyAvatarAddress, };
            foreach (var avatarAddress in addresses)
            {
                var itemSlotStateAddress = ItemSlotState.DeriveAddress(avatarAddress, BattleType.Arena);
                Assert.Null(_initialState.GetLegacyState(itemSlotStateAddress));

                var avatarState = prevStates.GetAvatarState(avatarAddress);
                for (var i = 0; i < 50; i++)
                {
                    avatarState.worldInformation.ClearStage(1, i + 1, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
                }

                prevStates = prevStates.SetAvatarState(
                    avatarAddress,
                    avatarState,
                    false,
                    false,
                    true,
                    false);

                var join = new JoinArena3
                {
                    avatarAddress = avatarAddress,
                    championshipId = 1,
                    round = 1,
                    costumes = new List<Guid>(),
                    equipments = new List<Guid>(),
                    runeInfos = new List<RuneSlotInfo>(),
                };
                var nextState = join.Execute(
                    new ActionContext
                    {
                        BlockIndex = 1,
                        Signer = _agentAddress,
                        PreviousState = prevStates,
                    });
                prevStates = nextState;
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
                    equipments = new List<Guid>(),
                    runeInfos = new List<RuneSlotInfo>(),
                };

                battle.Execute(
                    new ActionContext
                    {
                        Signer = _agentAddress,
                        PreviousState = prevStates,
                        BlockIndex = 2,
                        RandomSeed = 0,
                    });
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EventDungeonBattle(bool collectionExist)
        {
            var states = _initialState;
            if (collectionExist)
            {
                var collectionState = new CollectionState();
                collectionState.Ids.Add(1);
                states = states.SetCollectionState(_avatarAddress, collectionState);
            }

            foreach (var (key, value) in _sheets)
            {
                if (key == nameof(CollectionSheet) && !collectionExist)
                {
                    continue;
                }

                states = states
                    .SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var scheduleRow = _tableSheets.EventScheduleSheet.Values.First();
            Assert.True(
                _tableSheets.EventDungeonSheet.TryGetRowByEventScheduleId(
                    scheduleRow.Id,
                    out var eventDungeonRow));
            var action = new EventDungeonBattle
            {
                AvatarAddress = _avatarAddress,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                RuneInfos = new List<RuneSlotInfo>(),
                Foods = new List<Guid>(),
                EventScheduleId = scheduleRow.Id,
                EventDungeonStageId = eventDungeonRow.StageBegin,
                EventDungeonId = eventDungeonRow.Id,
            };

            action.Execute(
                new ActionContext
                {
                    PreviousState = states,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                    BlockIndex = scheduleRow.StartBlockIndex,
                });
        }
    }
}
