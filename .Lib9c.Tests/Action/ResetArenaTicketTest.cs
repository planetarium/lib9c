namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class ResetArenaTicketTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        private readonly Address _agent1Address;
        private readonly Address _avatar1Address;
        private readonly AvatarState _avatar1;
        private readonly Currency _currency;
        private IAccountStateDelta _state;

        public ResetArenaTicketTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _state = new State();

            _sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(_sheets);
            foreach (var (key, value) in _sheets)
            {
                _state = _state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(_sheets);
            _currency = new Currency("CRYSTAL", 18, minters: null);
            var rankingMapAddress = new PrivateKey().ToAddress();
            var clearStageId = Math.Max(
                _tableSheets.StageSheet.First?.Id ?? 1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard);

            var (agent1State, avatar1State) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                rankingMapAddress,
                clearStageId);

            _agent1Address = agent1State.address;
            _avatar1 = avatar1State;
            _avatar1Address = avatar1State.address;
            _state = _state
                .SetState(_agent1Address, agent1State.Serialize())
                .SetState(_avatar1Address.Derive(LegacyInventoryKey), _avatar1.inventory.Serialize())
                .SetState(_avatar1Address.Derive(LegacyWorldInformationKey), _avatar1.worldInformation.Serialize())
                .SetState(_avatar1Address.Derive(LegacyQuestListKey), _avatar1.questList.Serialize())
                .SetState(_avatar1Address, _avatar1.Serialize())
                .SetState(Addresses.GameConfig, new GameConfigState(_sheets[nameof(GameConfigSheet)]).Serialize());

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
        }

        public static (AgentState AgentState, AvatarState AvatarState) GetAgentStateWithAvatarState(
            IReadOnlyDictionary<string, string> sheets,
            TableSheets tableSheets,
            Address rankingMapAddress,
            int clearStageId)
        {
            var agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(sheets[nameof(GameConfigSheet)]),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    tableSheets.WorldSheet,
                    clearStageId),
            };
            agentState.avatarAddresses.Add(0, avatarAddress);

            return (agentState, avatarState);
        }

        public IAccountStateDelta JoinArena(Address signer, Address avatarAddress, long blockIndex, int championshipId, int round, IRandom random)
        {
            var preCurrency = 1000 * _currency;
            _state = _state.MintAsset(signer, preCurrency);

            var action = new JoinArena()
            {
                championshipId = championshipId,
                round = round,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                avatarAddress = avatarAddress,
            };

            _state = action.Execute(new ActionContext
            {
                PreviousStates = _state,
                Signer = signer,
                Random = random,
                Rehearsal = false,
                BlockIndex = blockIndex,
            });
            return _state;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Execute(long nextBlockIndex)
        {
            // todo : When the arena sheet is filled, you need to edit the inline data.
            var championshipId = 1;
            var round = 1;
            var usedTicket = 2;
            var arenaSheet = _state.GetSheet<ArenaSheet>();
            if (!arenaSheet.TryGetValue(championshipId, out var row))
            {
                throw new SheetRowNotFoundException(
                    nameof(ArenaSheet), $"championship Id : {championshipId}");
            }

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.Id}) - round({round})");
            }

            var random = new TestRandom();
            _state = JoinArena(_agent1Address, _avatar1Address, roundData.StartBlockIndex, roundData.Id, roundData.Round, random);
            var aiAdr = ArenaInformation.DeriveAddress(_avatar1Address, roundData.Id, roundData.Round);
            if (!_state.TryGetArenaInformation(aiAdr, out var beforeArenaInfo))
            {
            }

            Assert.Equal(ArenaInformation.MaxTicketCount, beforeArenaInfo.Ticket);
            beforeArenaInfo.UseTicket(usedTicket);
            Assert.Equal(ArenaInformation.MaxTicketCount - usedTicket, beforeArenaInfo.Ticket);
            Assert.Equal(0, beforeArenaInfo.TicketResetCount);

            var action = new ResetArenaTicket()
            {
                avatarAddress = _avatar1Address,
            };

            var blockIndex = roundData.StartBlockIndex + nextBlockIndex;
            var interval = _state.GetGameConfigState().DailyArenaInterval;
            var diff = blockIndex - roundData.StartBlockIndex;
            var result = diff / interval;
            if (result == 0)
            {
                return;
            }

            _state = action.Execute(new ActionContext
            {
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = random,
                Rehearsal = false,
                BlockIndex = blockIndex,
            });

            if (!_state.TryGetArenaInformation(aiAdr, out var afterArenaInfo))
            {
            }

            if (afterArenaInfo.TicketResetCount > beforeArenaInfo.TicketResetCount)
            {
                Assert.Equal(ArenaInformation.MaxTicketCount, afterArenaInfo.Ticket);
            }
            else
            {
                Assert.Equal(ArenaInformation.MaxTicketCount - usedTicket, afterArenaInfo.Ticket);
            }

            Assert.Equal(result, afterArenaInfo.TicketResetCount);
        }

        [Fact]
        public void Execute_FailedLoadStateException()
        {
            var action = new ResetArenaTicket()
            {
                avatarAddress = _agent1Address,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_RoundNotFoundException()
        {
            var random = new TestRandom();
            var action = new ResetArenaTicket()
            {
                avatarAddress = _avatar1Address,
            };

            Assert.Throws<RoundNotFoundException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = random,
                Rehearsal = false,
                BlockIndex = 987243058724,
            }));
        }

        [Fact]
        public void Execute_ArenaInformationNotFoundException()
        {
            var championshipId = 1;
            var round = 1;
            var arenaSheet = _state.GetSheet<ArenaSheet>();
            if (!arenaSheet.TryGetValue(championshipId, out var row))
            {
                throw new SheetRowNotFoundException(
                    nameof(ArenaSheet), $"championship Id : {championshipId}");
            }

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.Id}) - round({round})");
            }

            var random = new TestRandom();
            var action = new ResetArenaTicket()
            {
                avatarAddress = _avatar1Address,
            };

            Assert.Throws<ArenaInformationNotFoundException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = random,
                Rehearsal = false,
                BlockIndex = roundData.StartBlockIndex + 1,
            }));
        }

        [Theory]
        [InlineData(1)]
        public void Execute_FailedToReachTicketResetBlockIndexException(long nextBlockIndex)
        {
            var championshipId = 1;
            var round = 1;
            var usedTicket = 2;
            var arenaSheet = _state.GetSheet<ArenaSheet>();
            if (!arenaSheet.TryGetValue(championshipId, out var row))
            {
                throw new SheetRowNotFoundException(
                    nameof(ArenaSheet), $"championship Id : {championshipId}");
            }

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.Id}) - round({round})");
            }

            var random = new TestRandom();
            _state = JoinArena(_agent1Address, _avatar1Address, roundData.StartBlockIndex, roundData.Id, roundData.Round, random);
            var aiAdr = ArenaInformation.DeriveAddress(_avatar1Address, roundData.Id, roundData.Round);
            if (!_state.TryGetArenaInformation(aiAdr, out var beforeArenaInfo))
            {
            }

            Assert.Equal(ArenaInformation.MaxTicketCount, beforeArenaInfo.Ticket);
            beforeArenaInfo.UseTicket(usedTicket);
            Assert.Equal(ArenaInformation.MaxTicketCount - usedTicket, beforeArenaInfo.Ticket);
            Assert.Equal(0, beforeArenaInfo.TicketResetCount);

            var action = new ResetArenaTicket()
            {
                avatarAddress = _avatar1Address,
            };

            Assert.Throws<FailedToReachTicketResetBlockIndexException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = random,
                Rehearsal = false,
                BlockIndex = roundData.StartBlockIndex + nextBlockIndex,
            }));
        }
    }
}
