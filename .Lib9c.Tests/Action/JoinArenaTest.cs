namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static SerializeKeys;

    public class JoinArenaTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;
        private readonly Address _signer;
        private readonly Address _avatarAddress;
        private readonly IRandom _random;
        private IAccountStateDelta _state;

        public JoinArenaTest(ITestOutputHelper outputHelper)
        {
            _random = new TestRandom();
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _signer = default;
            _avatarAddress = _signer.Derive("avatar");
            _state = new State();
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var rankingMapAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_signer);
            var avatarState = new AvatarState(
                _avatarAddress,
                _signer,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress);
            agentState.avatarAddresses[0] = _avatarAddress;

            var currency = new Currency("NCG", 2, minters: null);
            var goldCurrencyState = new GoldCurrencyState(currency);

            _state = _state
                .SetState(_signer, agentState.Serialize())
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            foreach ((string key, string value) in sheets)
            {
                _state = _state
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        public (List<Guid> Equipments, List<Guid> Costumes) GetDummyItems(AvatarState avatarState)
        {
            var items = Doomfist.GetAllParts(_tableSheets, avatarState.level);
            foreach (var equipment in items)
            {
                avatarState.inventory.AddItem(equipment);
            }

            var equipments = items.Select(e => e.NonFungibleId).ToList();

            var random = new TestRandom();
            var costumes = new List<Guid>();
            if (avatarState.level >= GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot)
            {
                var costumeId = _tableSheets
                    .CostumeItemSheet
                    .Values
                    .First(r => r.ItemSubType == ItemSubType.FullCostume)
                    .Id;

                var costume = (Costume)ItemFactory.CreateItem(
                    _tableSheets.ItemSheet[costumeId], random);
                avatarState.inventory.AddItem(costume);
                costumes.Add(costume.ItemId);
            }

            return (equipments, costumes);
        }

        public AvatarState GetAvatarState(bool backward, out List<Guid> equipments, out List<Guid> costumes)
        {
            var avatarState = _state.GetAvatarStateV2(_avatarAddress);

            IAccountStateDelta state;
            if (backward)
            {
                state = _state.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                state = _state
                    .SetState(_avatarAddress, avatarState.SerializeV2())
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize());
            }

            avatarState.level = 999;
            (equipments, costumes) = GetDummyItems(avatarState);
            return avatarState;
        }

        [Theory]
        [InlineData(true, 0, 1, 1)]
        [InlineData(false, 0, 1, 1)]
        public void Execute(bool backward, long blockIndex, int championshipId, int round)
        {
            var avatarState = GetAvatarState(backward, out var equipments, out var costumes);
            var state = _state.SetState(_avatarAddress, avatarState.SerializeV2());

            var action = new JoinArena()
            {
                championshipId = championshipId,
                round = round,
                costumes = costumes,
                equipments = equipments,
                avatarAddress = _avatarAddress,
            };

            state = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _signer,
                Random = _random,
                Rehearsal = false,
                BlockIndex = blockIndex,
            });

            var arenaSheet = _state.GetSheet<ArenaSheet>();
            if (!arenaSheet.TryGetValue(championshipId, out var row))
            {
                throw new SheetRowNotFoundException(
                    nameof(ArenaSheet), $"championship Id : {championshipId}");
            }

            if (!row.TryGetRound(blockIndex, championshipId, round, out var roundData))
            {
                throw new RoundDoesNotExistException($"{nameof(JoinArena)} : {blockIndex}");
            }

            // ArenaParticipants
            var arenaParticipantsAdr = ArenaParticipants.DeriveAddress(championshipId, round);
            var serializedArenaParticipants = (List)state.GetState(arenaParticipantsAdr);
            var arenaParticipants = new ArenaParticipants(serializedArenaParticipants);

            Assert.Equal(arenaParticipantsAdr, arenaParticipants.Address);
            Assert.Equal(_avatarAddress, arenaParticipants.AvatarAddresses.First());

            // ArenaAvatarState
            var arenaAvatarStateAdr = ArenaAvatarState.DeriveAddress(_avatarAddress);
            var serializedArenaAvatarState = (List)state.GetState(arenaAvatarStateAdr);
            var arenaAvatarState = new ArenaAvatarState(serializedArenaAvatarState);

            foreach (var guid in arenaAvatarState.Equipments)
            {
                Assert.Contains(avatarState.inventory.Equipments, x => x.ItemId.Equals(guid));
            }

            foreach (var guid in arenaAvatarState.Costumes)
            {
                Assert.Contains(avatarState.inventory.Costumes, x => x.ItemId.Equals(guid));
            }

            Assert.Equal(arenaAvatarStateAdr, arenaAvatarState.Address);
            Assert.Equal(avatarState.level, arenaAvatarState.Level);

            // ArenaScore
            var arenaScoreAdr = ArenaScore.DeriveAddress(_avatarAddress, championshipId, round);
            var serializedArenaScore = (List)state.GetState(arenaScoreAdr);
            var arenaScore = new ArenaScore(serializedArenaScore);

            Assert.Equal(arenaScoreAdr, arenaScore.Address);
            Assert.Equal(GameConfig.ArenaScoreDefault, arenaScore.Score);

            // Arenainfor
            var arenaInformationAdr = ArenaInformation.DeriveAddress(_avatarAddress, championshipId, round);
            var serializedArenaInformation = (List)state.GetState(arenaInformationAdr);
            var arenaInformation = new ArenaInformation(serializedArenaInformation);

            Assert.Equal(arenaInformationAdr, arenaInformation.Address);
            Assert.Equal(0, arenaInformation.Win);
            Assert.Equal(0, arenaInformation.Lose);
            Assert.Equal(GameConfig.ArenaChallengeCountMax, arenaInformation.Ticket);
        }

        [Theory]
        [InlineData(true, 9999)]
        [InlineData(false, 9999)]
        public void Execute_SheetRowNotFoundException(bool backward, int championshipId)
        {
            var avatarState = GetAvatarState(backward, out var equipments, out var costumes);
            var state = _state.SetState(_avatarAddress, avatarState.SerializeV2());

            var action = new JoinArena()
            {
                championshipId = championshipId,
                round = 1,
                costumes = costumes,
                equipments = equipments,
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _signer,
                Random = new TestRandom(),
            }));
        }

        [Theory]
        [InlineData(true, 9999999999)]
        [InlineData(false, 9999999999)]
        public void Execute_RoundDoesNotExistException(bool backward, long blockIndex)
        {
            var avatarState = GetAvatarState(backward, out var equipments, out var costumes);
            var state = _state.SetState(_avatarAddress, avatarState.SerializeV2());

            var action = new JoinArena()
            {
                championshipId = 1,
                round = 1,
                costumes = costumes,
                equipments = equipments,
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<RoundDoesNotExistException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = blockIndex,
            }));
        }

        [Theory]
        [InlineData(true, 123)]
        [InlineData(false, 123)]
        public void Execute_RoundDoNotMatchException(bool backward, int round)
        {
            var avatarState = GetAvatarState(backward, out var equipments, out var costumes);
            var state = _state.SetState(_avatarAddress, avatarState.SerializeV2());

            var action = new JoinArena()
            {
                championshipId = 1,
                round = round,
                costumes = costumes,
                equipments = equipments,
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<RoundDoNotMatchException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = 1,
            }));
        }

        [Theory]
        [InlineData(true, 3)]
        [InlineData(false, 3)]
        public void Execute_NotEnoughMedalException(bool backward, int round)
        {
            var avatarState = GetAvatarState(backward, out var equipments, out var costumes);
            var state = _state.SetState(_avatarAddress, avatarState.SerializeV2());

            var action = new JoinArena()
            {
                championshipId = 1,
                round = round,
                costumes = costumes,
                equipments = equipments,
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<NotEnoughMedalException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = 100,
            }));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_ArenaScoreAlreadyContainsException(bool backward)
        {
            var avatarState = GetAvatarState(backward, out var equipments, out var costumes);
            var state = _state.SetState(_avatarAddress, avatarState.SerializeV2());

            var action = new JoinArena()
            {
                championshipId = 1,
                round = 1,
                costumes = costumes,
                equipments = equipments,
                avatarAddress = _avatarAddress,
            };

            state = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _signer,
                Random = _random,
                Rehearsal = false,
                BlockIndex = 1,
            });

            Assert.Throws<ArenaScoreAlreadyContainsException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = 2,
            }));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_ArenaScoreAlreadyContainsException2(bool backward)
        {
            const int championshipId = 1;
            const int round = 1;

            var avatarState = GetAvatarState(backward, out var equipments, out var costumes);
            var state = _state.SetState(_avatarAddress, avatarState.SerializeV2());

            var arenaScoreAdr = ArenaScore.DeriveAddress(_avatarAddress, championshipId, round);
            var arenaScore = new ArenaScore(_avatarAddress, championshipId, round);
            state = state.SetState(arenaScoreAdr, arenaScore.Serialize());

            var action = new JoinArena()
            {
                championshipId = championshipId,
                round = round,
                costumes = costumes,
                equipments = equipments,
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<ArenaScoreAlreadyContainsException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = 1,
            }));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_ArenaInformationAlreadyContainsException(bool backward)
        {
            const int championshipId = 1;
            const int round = 1;

            var avatarState = GetAvatarState(backward, out var equipments, out var costumes);
            var state = _state.SetState(_avatarAddress, avatarState.SerializeV2());

            var arenaInformationAdr = ArenaInformation.DeriveAddress(_avatarAddress, championshipId, round);
            var arenaInformation = new ArenaInformation(_avatarAddress, championshipId, round);
            state = state.SetState(arenaInformationAdr, arenaInformation.Serialize());

            var action = new JoinArena()
            {
                championshipId = championshipId,
                round = round,
                costumes = costumes,
                equipments = equipments,
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<ArenaInformationAlreadyContainsException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = 1,
            }));
        }
    }
}
