namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Arena;
    using Nekoyume.Model;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class BattleArena4Test
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        private readonly Address _agent1Address;
        private readonly Address _agent2Address;
        private readonly Address _agent3Address;
        private readonly Address _agent4Address;
        private readonly Address _avatar1Address;
        private readonly Address _avatar2Address;
        private readonly Address _avatar3Address;
        private readonly Address _avatar4Address;
        private readonly AvatarState _avatar1;
        private readonly AvatarState _avatar2;
        private readonly AvatarState _avatar3;
        private readonly AvatarState _avatar4;
        private readonly Currency _crystal;
        private readonly Currency _ncg;
        private IAccountStateDelta _state;

        public BattleArena4Test(ITestOutputHelper outputHelper)
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
            _crystal = new Currency("CRYSTAL", 18, minters: null);
            _ncg = new Currency("NCG", 2, minters: null);
            var goldCurrencyState = new GoldCurrencyState(_ncg);

            var rankingMapAddress = new PrivateKey().ToAddress();
            var clearStageId = Math.Max(
                _tableSheets.StageSheet.First?.Id ?? 1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard);

            // account 1
            var (agent1State, avatar1State) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                rankingMapAddress,
                clearStageId);

            _agent1Address = agent1State.address;
            _avatar1 = avatar1State;
            _avatar1Address = avatar1State.address;

            // account 2
            var (agent2State, avatar2State) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                rankingMapAddress,
                clearStageId);
            _agent2Address = agent2State.address;
            _avatar2 = avatar2State;
            _avatar2Address = avatar2State.address;

            // account 3
            var (agent3State, avatar3State) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                rankingMapAddress,
                1);
            _agent3Address = agent3State.address;
            _avatar3 = avatar3State;
            _avatar3Address = avatar3State.address;

            // account 4
            var (agent4State, avatar4State) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                rankingMapAddress,
                1);

            _agent4Address = agent4State.address;
            _avatar4 = avatar4State;
            _avatar4Address = avatar4State.address;

            _state = _state
                .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
                .SetState(_agent1Address, agent1State.Serialize())
                .SetState(_avatar1Address.Derive(LegacyInventoryKey), _avatar1.inventory.Serialize())
                .SetState(_avatar1Address.Derive(LegacyWorldInformationKey), _avatar1.worldInformation.Serialize())
                .SetState(_avatar1Address.Derive(LegacyQuestListKey), _avatar1.questList.Serialize())
                .SetState(_avatar1Address, _avatar1.Serialize())
                .SetState(_agent2Address, agent2State.Serialize())
                .SetState(_avatar2Address, avatar2State.Serialize())
                .SetState(_agent3Address, agent3State.Serialize())
                .SetState(_avatar3Address, avatar3State.Serialize())
                .SetState(_agent4Address, agent4State.Serialize())
                .SetState(_avatar4Address.Derive(LegacyInventoryKey), _avatar4.inventory.Serialize())
                .SetState(_avatar4Address.Derive(LegacyWorldInformationKey), _avatar4.worldInformation.Serialize())
                .SetState(_avatar4Address.Derive(LegacyQuestListKey), _avatar4.questList.Serialize())
                .SetState(_avatar4Address, avatar4State.Serialize())
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

        public IAccountStateDelta JoinArena(Address signer, Address avatarAddress, long blockIndex, int championshipId, int round, IRandom random)
        {
            var preCurrency = 1000 * _crystal;
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
        [InlineData(1, 1, 1, false, 1, 2, 3)]
        [InlineData(1, 1, 1, false, 1, 2, 4)]
        [InlineData(1, 1, 1, false, 5, 2, 3)]
        [InlineData(1, 1, 1, true, 1, 2, 3)]
        [InlineData(1, 1, 1, true, 3, 2, 3)]
        [InlineData(1, 1, 2, false, 1, 2, 3)]
        [InlineData(1, 1, 2, true, 1, 2, 3)]
        public void Execute(
            long nextBlockIndex,
            int championshipId,
            int round,
            bool isPurchased,
            int ticket,
            int arenaInterval,
            int randomSeed)
        {
            Assert.True(_state.GetSheet<ArenaSheet>().TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena4)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom(randomSeed);
            _state = JoinArena(_agent1Address, _avatar1Address, roundData.StartBlockIndex, championshipId, round, random);
            _state = JoinArena(_agent2Address, _avatar2Address, roundData.StartBlockIndex, championshipId, round, random);

            var arenaInfoAdr = ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!_state.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            if (isPurchased)
            {
                beforeInfo.UseTicket(beforeInfo.Ticket);
                _state = _state.SetState(arenaInfoAdr, beforeInfo.Serialize());
                for (var i = 0; i < ticket; i++)
                {
                    var price = ArenaHelper.GetTicketPrice(roundData, beforeInfo, _state.GetGoldCurrency());
                    _state = _state.MintAsset(_agent1Address, price);
                    beforeInfo.BuyTicket(roundData);
                }
            }

            var beforeBalance = _state.GetBalance(_agent1Address, _state.GetGoldCurrency());

            var action = new BattleArena4()
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = ticket,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            var myScoreAdr = ArenaScore.DeriveAddress(_avatar1Address, championshipId, round);
            var enemyScoreAdr = ArenaScore.DeriveAddress(_avatar2Address, championshipId, round);
            if (!_state.TryGetArenaScore(myScoreAdr, out var beforeMyScore))
            {
                throw new ArenaScoreNotFoundException($"myScoreAdr : {myScoreAdr}");
            }

            if (!_state.TryGetArenaScore(enemyScoreAdr, out var beforeEnemyScore))
            {
                throw new ArenaScoreNotFoundException($"enemyScoreAdr : {enemyScoreAdr}");
            }

            Assert.Empty(_avatar1.inventory.Materials);

            var gameConfigState = SetArenaInterval(arenaInterval);
            _state = _state.SetState(GameConfigState.Address, gameConfigState.Serialize());

            var blockIndex = roundData.StartBlockIndex + nextBlockIndex;
            _state = action.Execute(new ActionContext
            {
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = random,
                Rehearsal = false,
                BlockIndex = blockIndex,
            });

            if (!_state.TryGetArenaScore(myScoreAdr, out var myAfterScore))
            {
                throw new ArenaScoreNotFoundException($"myScoreAdr : {myScoreAdr}");
            }

            if (!_state.TryGetArenaScore(enemyScoreAdr, out var enemyAfterScore))
            {
                throw new ArenaScoreNotFoundException($"enemyScoreAdr : {enemyScoreAdr}");
            }

            if (!_state.TryGetArenaInformation(arenaInfoAdr, out var afterInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            var (myWinScore, myDefeatScore, enemyWinScore) =
                ArenaHelper.GetScores(beforeMyScore.Score, beforeEnemyScore.Score);

            var addMyScore = (afterInfo.Win * myWinScore) + (afterInfo.Lose * myDefeatScore);
            var addEnemyScore = afterInfo.Win * enemyWinScore;
            var expectedMyScore = Math.Max(beforeMyScore.Score + addMyScore, ArenaScore.ArenaScoreDefault);
            var expectedEnemyScore = Math.Max(beforeEnemyScore.Score + addEnemyScore, ArenaScore.ArenaScoreDefault);

            Assert.Equal(expectedMyScore, myAfterScore.Score);
            Assert.Equal(expectedEnemyScore, enemyAfterScore.Score);
            Assert.Equal(isPurchased ? 0 : ArenaInformation.MaxTicketCount, beforeInfo.Ticket);
            Assert.Equal(0, beforeInfo.Win);
            Assert.Equal(0, beforeInfo.Lose);

            var useTicket = Math.Min(ticket, beforeInfo.Ticket);
            Assert.Equal(beforeInfo.Ticket - useTicket, afterInfo.Ticket);
            Assert.Equal(ticket, afterInfo.Win + afterInfo.Lose);

            var balance = _state.GetBalance(_agent1Address, _state.GetGoldCurrency());
            if (isPurchased)
            {
                Assert.Equal(ticket, afterInfo.PurchasedTicketCount);
            }

            Assert.Equal(0, balance.RawValue);

            var avatarState = _state.GetAvatarStateV2(_avatar1Address);
            var medalCount = 0;
            if (roundData.ArenaType != ArenaType.OffSeason)
            {
                var medalId = ArenaHelper.GetMedalItemId(championshipId, round);
                avatarState.inventory.TryGetItem(medalId, out var medal);
                if (afterInfo.Win > 0)
                {
                    Assert.Equal(afterInfo.Win, medal.count);
                }
                else
                {
                    Assert.Null(medal);
                }

                medalCount = medal?.count ?? 0;
            }

            var materialCount = avatarState.inventory.Materials.Count();
            var high = (ArenaHelper.GetRewardCount(beforeMyScore.Score) * ticket) + medalCount;
            Assert.InRange(materialCount, 0, high);
        }

        public GameConfigState SetArenaInterval(int interval)
        {
            var gameConfigState = _state.GetGameConfigState();
            var sheet = _tableSheets.GameConfigSheet;
            foreach (var value in sheet.Values)
            {
                if (value.Key.Equals("daily_arena_interval"))
                {
                    IReadOnlyList<string> field = new[]
                    {
                        value.Key,
                        interval.ToString(),
                    };
                    value.Set(field);
                }
            }

            gameConfigState.Set(sheet);
            return gameConfigState;
        }

        [Fact]
        public void Execute_InvalidAddressException()
        {
            var action = new BattleArena4()
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar1Address,
                championshipId = 1,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            Assert.Throws<InvalidAddressException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_FailedLoadStateException()
        {
            var action = new BattleArena4()
            {
                myAvatarAddress = _avatar2Address,
                enemyAvatarAddress = _avatar1Address,
                championshipId = 1,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_NotEnoughClearedStageLevelException()
        {
            var action = new BattleArena4()
            {
                myAvatarAddress = _avatar4Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = 1,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _state,
                Signer = _agent4Address,
                Random = new TestRandom(),
                BlockIndex = 1,
            }));
        }

        [Fact]
        public void Execute_SheetRowNotFoundException()
        {
            var action = new BattleArena4()
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = 9999999,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_ThisArenaIsClosedException()
        {
            var action = new BattleArena4()
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = 1,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            Assert.Throws<ThisArenaIsClosedException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = new TestRandom(),
                BlockIndex = 4480001,
            }));
        }

        [Fact]
        public void Execute_ArenaParticipantsNotFoundException()
        {
            var action = new BattleArena4()
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = 1,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            Assert.Throws<ArenaParticipantsNotFoundException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = new TestRandom(),
                BlockIndex = 1,
            }));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_AddressNotFoundInArenaParticipantsException(bool excludeMe)
        {
            var championshipId = 1;
            var round = 1;

            Assert.True(_state.GetSheet<ArenaSheet>().TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena4)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            _state = excludeMe
                ? JoinArena(_agent2Address, _avatar2Address, roundData.StartBlockIndex, championshipId, round, random)
                : JoinArena(_agent1Address, _avatar1Address, roundData.StartBlockIndex, championshipId, round, random);

            var action = new BattleArena4()
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            Assert.Throws<AddressNotFoundInArenaParticipantsException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = new TestRandom(),
                BlockIndex = 1,
            }));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_ValidateScoreDifferenceException(bool isSigner)
        {
            var championshipId = 1;
            var round = 2;

            Assert.True(_state.GetSheet<ArenaSheet>().TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena4)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            _state = JoinArena(_agent1Address, _avatar1Address, roundData.StartBlockIndex, championshipId, round, random);
            _state = JoinArena(_agent2Address, _avatar2Address, roundData.StartBlockIndex, championshipId, round, random);

            var arenaScoreAdr = ArenaScore.DeriveAddress(isSigner ? _avatar1Address : _avatar2Address, roundData.ChampionshipId, roundData.Round);
            _state.TryGetArenaScore(arenaScoreAdr, out var arenaScore);
            arenaScore.AddScore(900);
            _state = _state.SetState(arenaScoreAdr, arenaScore.Serialize());

            var action = new BattleArena4()
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;
            Assert.Throws<ValidateScoreDifferenceException>(() => action.Execute(new ActionContext()
            {
                BlockIndex = blockIndex,
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_InsufficientBalanceException()
        {
            var championshipId = 1;
            var round = 2;

            Assert.True(_state.GetSheet<ArenaSheet>().TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena4)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            _state = JoinArena(_agent1Address, _avatar1Address, roundData.StartBlockIndex, championshipId, round, random);
            _state = JoinArena(_agent2Address, _avatar2Address, roundData.StartBlockIndex, championshipId, round, random);

            var arenaInfoAdr = ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!_state.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            beforeInfo.UseTicket(beforeInfo.Ticket);
            _state = _state.SetState(arenaInfoAdr, beforeInfo.Serialize());

            var action = new BattleArena4()
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;
            Assert.Throws<InsufficientBalanceException>(() => action.Execute(new ActionContext()
            {
                BlockIndex = blockIndex,
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_ExceedPlayCountException()
        {
            var championshipId = 1;
            var round = 2;

            Assert.True(_state.GetSheet<ArenaSheet>().TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena4)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            _state = JoinArena(_agent1Address, _avatar1Address, roundData.StartBlockIndex, championshipId, round, random);
            _state = JoinArena(_agent2Address, _avatar2Address, roundData.StartBlockIndex, championshipId, round, random);

            var arenaInfoAdr = ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!_state.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            var action = new BattleArena4()
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 2,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;
            Assert.Throws<ExceedPlayCountException>(() => action.Execute(new ActionContext()
            {
                BlockIndex = blockIndex,
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_ExceedTicketPurchaseLimitException()
        {
            var championshipId = 1;
            var round = 2;

            Assert.True(_state.GetSheet<ArenaSheet>().TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena4)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            _state = JoinArena(_agent1Address, _avatar1Address, roundData.StartBlockIndex, championshipId, round, random);
            _state = JoinArena(_agent2Address, _avatar2Address, roundData.StartBlockIndex, championshipId, round, random);

            var arenaInfoAdr = ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!_state.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            beforeInfo.UseTicket(ArenaInformation.MaxTicketCount);
            var max = ArenaHelper.GetMaxPurchasedTicketCount(roundData);
            for (var i = 0; i < max; i++)
            {
                beforeInfo.BuyTicket(roundData);
            }

            _state = _state.SetState(arenaInfoAdr, beforeInfo.Serialize());
            var price = ArenaHelper.GetTicketPrice(roundData, beforeInfo, _state.GetGoldCurrency());
            _state = _state.MintAsset(_agent1Address, price);

            var action = new BattleArena4()
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;
            Assert.Throws<ExceedTicketPurchaseLimitException>(() => action.Execute(new ActionContext()
            {
                BlockIndex = blockIndex,
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_CoolDownBlockException()
        {
            var championshipId = 1;
            var round = 2;

            Assert.True(_state.GetSheet<ArenaSheet>().TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena4)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            _state = JoinArena(_agent1Address, _avatar1Address, roundData.StartBlockIndex, championshipId, round, random);
            _state = JoinArena(_agent2Address, _avatar2Address, roundData.StartBlockIndex, championshipId, round, random);

            var arenaInfoAdr = ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!_state.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            beforeInfo.UseTicket(ArenaInformation.MaxTicketCount);
            var max = ArenaHelper.GetMaxPurchasedTicketCount(roundData);
            _state = _state.SetState(arenaInfoAdr, beforeInfo.Serialize());
            for (var i = 0; i < max; i++)
            {
                var price = ArenaHelper.GetTicketPrice(roundData, beforeInfo, _state.GetGoldCurrency());
                _state = _state.MintAsset(_agent1Address, price);
                beforeInfo.BuyTicket(roundData);
            }

            var action = new BattleArena4()
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;

            var newState = action.Execute(new ActionContext()
            {
                BlockIndex = blockIndex,
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = new TestRandom(),
            });

            Assert.Throws<CoolDownBlockException>(() => action.Execute(new ActionContext()
            {
                BlockIndex = blockIndex + 1,
                PreviousStates = newState,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_v100291()
        {
            const string csv =
                @"id,group,_name,chance,duration,target_type,stat_type,modify_type,modify_value,icon_resource
101000,101000,체력 강화,20,10,Self,HP,Percentage,50,icon_buff_plus_hp
101001,101000,체력 강화,100,25,Self,HP,Percentage,50,icon_buff_plus_hp
102000,102000,공격 강화,20,10,Self,ATK,Percentage,25,icon_buff_plus_attack
102001,102000,공격 강화,100,25,Self,ATK,Percentage,50,icon_buff_plus_attack
102002,102000,공격 강화,100,10,Self,ATK,Percentage,25,icon_buff_plus_attack
103000,103000,방어 강화,20,10,Self,DEF,Percentage,25,icon_buff_plus_defense
103001,103000,방어 강화,100,25,Self,DEF,Percentage,50,icon_buff_plus_defense
103002,103000,방어 강화,100,10,Self,DEF,Percentage,25,icon_buff_plus_defense
104000,104000,치명 증가,20,10,Self,CRI,Percentage,50,icon_buff_plus_critical
104001,104000,치명 증가,100,25,Self,CRI,Percentage,75,icon_buff_plus_critical
104002,104000,치명 증가,100,10,Self,CRI,Percentage,25,icon_buff_plus_critical
105000,105000,회피 증가,20,10,Self,HIT,Percentage,50,icon_buff_plus_hit
105001,105000,회피 증가,100,25,Self,HIT,Percentage,75,icon_buff_plus_hit
105002,105000,회피 증가,100,10,Self,HIT,Percentage,25,icon_buff_plus_hit
106000,106000,속도 증가,20,10,Self,SPD,Percentage,50,icon_buff_plus_speed
106001,106000,속도 증가,100,25,Self,SPD,Percentage,75,icon_buff_plus_speed
106002,106000,속도 증가,100,10,Self,SPD,Percentage,25,icon_buff_plus_speed
202000,202000,공격 약화,20,10,Enemy,ATK,Percentage,-25,icon_buff_minus_attack
202001,202000,공격 약화,100,10,Enemy,ATK,Percentage,-25,icon_buff_minus_attack
203001,203000,방어 약화,100,10,Enemy,DEF,Percentage,-25,icon_buff_minus_defense
204001,204000,치명 감소,100,10,Enemy,CRI,Percentage,-25,icon_buff_minus_critical
205001,205000,회피 감소,100,10,Enemy,HIT,Percentage,-25,icon_buff_minus_hit
206001,206000,속도 감소,100,10,Enemy,SPD,Percentage,-25,icon_buff_minus_speed
301000,301000,체력 강화 (10),100,150,Self,HP,Percentage,10,icon_buff_plus_hp
302000,302000,공격 강화 (2),100,150,Self,ATK,Percentage,2,icon_buff_plus_attack
302001,302000,공격 강화 (2),100,150,Self,ATK,Percentage,2,icon_buff_plus_attack
302002,302000,공격 강화 (3),100,150,Self,ATK,Percentage,3,icon_buff_plus_attack
302003,302000,공격 강화 (6),100,150,Self,ATK,Percentage,6,icon_buff_plus_attack
302004,302000,공격 강화 (3),100,150,Self,ATK,Percentage,3,icon_buff_plus_attack
302005,302000,공격 강화 (5),100,150,Self,ATK,Percentage,5,icon_buff_plus_attack
302006,302000,공격 강화 (8),100,150,Self,ATK,Percentage,8,icon_buff_plus_attack
302007,302000,S 올스텟,100,150,Self,ATK,Percentage,20,icon_buff_plus_attack
302008,302000,S 공격력2,100,150,Self,ATK,Percentage,35,icon_buff_plus_attack
302009,302000,공격 강화 (18),100,150,Self,ATK,Percentage,18,icon_buff_plus_attack
302010,302000,SS 올스탯,100,150,Self,ATK,Percentage,50,icon_buff_plus_attack
302011,302000,S 공격력1,100,150,Self,ATK,Percentage,60,icon_buff_plus_attack
302012,302000,SS 공격력,100,150,Self,ATK,Percentage,90,icon_buff_plus_attack
303000,303000,방어 강화 (2),100,150,Self,DEF,Percentage,2,icon_buff_plus_defense
303001,303000,방어 강화 (2),100,150,Self,DEF,Percentage,2,icon_buff_plus_defense
303002,303000,방어 강화 (3),100,150,Self,DEF,Percentage,3,icon_buff_plus_defense
303003,303000,방어 강화 (6),100,150,Self,DEF,Percentage,6,icon_buff_plus_defense
303004,303000,방어 강화 (3),100,150,Self,DEF,Percentage,3,icon_buff_plus_defense
303005,303000,방어 강화 (5),100,150,Self,DEF,Percentage,5,icon_buff_plus_defense
303006,303000,방어 강화 (8),100,150,Self,DEF,Percentage,8,icon_buff_plus_defense
303007,303000,S 올스텟,100,150,Self,DEF,Percentage,20,icon_buff_plus_defense
303008,303000,S 방어력2,100,150,Self,DEF,Percentage,35,icon_buff_plus_defense
303009,303000,방어 강화 (18),100,150,Self,DEF,Percentage,18,icon_buff_plus_defense
303010,303000,SS 올스탯,100,150,Self,DEF,Percentage,50,icon_buff_plus_defense
303011,303000,S 방어력1,100,150,Self,DEF,Percentage,60,icon_buff_plus_defense
303012,303000,SS 방어력,100,150,Self,DEF,Percentage,90,icon_buff_plus_defense
304000,304000,치명 증가 (100),100,150,Self,CRI,Percentage,100,icon_buff_plus_critical
304001,304000,치명 증가 (250),100,150,Self,CRI,Percentage,250,icon_buff_plus_critical
305000,305000,명중 강화 (2),100,150,Self,HIT,Percentage,2,icon_buff_plus_hit
305001,305000,명중 강화 (2),100,150,Self,HIT,Percentage,2,icon_buff_plus_hit
305002,305000,명중 강화 (3),100,150,Self,HIT,Percentage,3,icon_buff_plus_hit
305003,305000,명중 강화 (6),100,150,Self,HIT,Percentage,6,icon_buff_plus_hit
305004,305000,명중 강화 (3),100,150,Self,HIT,Percentage,3,icon_buff_plus_hit
305005,305000,명중 강화 (5),100,150,Self,HIT,Percentage,5,icon_buff_plus_hit
305006,305000,명중 강화 (8),100,150,Self,HIT,Percentage,8,icon_buff_plus_hit
305007,305000,S 올스텟,100,150,Self,HIT,Percentage,20,icon_buff_plus_hit
305008,305000,S 명중2,100,150,Self,HIT,Percentage,35,icon_buff_plus_hit
305009,305000,명중 강화 (18),100,150,Self,HIT,Percentage,18,icon_buff_plus_hit
305010,305000,SS 올스탯,100,150,Self,HIT,Percentage,50,icon_buff_plus_hit
305011,305000,S 명중1,100,150,Self,HIT,Percentage,60,icon_buff_plus_hit
305012,305000,SS 명중,100,150,Self,HIT,Percentage,90,icon_buff_plus_hit
306000,306000,속도 강화 (2),100,150,Self,SPD,Percentage,2,icon_buff_plus_speed
306001,306000,속도 강화 (2),100,150,Self,SPD,Percentage,2,icon_buff_plus_speed
306002,306000,속도 강화 (3),100,150,Self,SPD,Percentage,3,icon_buff_plus_speed
306003,306000,속도 강화 (6),100,150,Self,SPD,Percentage,6,icon_buff_plus_speed
306004,306000,속도 강화 (3),100,150,Self,SPD,Percentage,3,icon_buff_plus_speed
306005,306000,속도 강화 (5),100,150,Self,SPD,Percentage,5,icon_buff_plus_speed
306006,306000,속도 강화 (8),100,150,Self,SPD,Percentage,8,icon_buff_plus_speed
306007,306000,S 올스텟,100,150,Self,SPD,Percentage,20,icon_buff_plus_speed
306008,306000,S 속도2,100,150,Self,SPD,Percentage,35,icon_buff_plus_speed
306009,306000,속도 강화 (18),100,150,Self,SPD,Percentage,18,icon_buff_plus_speed
306010,306000,SS 올스탯,100,150,Self,SPD,Percentage,50,icon_buff_plus_speed
306011,306000,S 속도1,100,150,Self,SPD,Percentage,60,icon_buff_plus_speed
306012,306000,SS 속도,100,150,Self,SPD,Percentage,90,icon_buff_plus_speed
501001,501001,공격 강화 (분노),100,5,Self,ATK,Percentage,50,icon_buff_plus_attack
502001,502001,속도 강화 (분노),100,5,Self,SPD,Percentage,50,icon_buff_plus_speed
503011,503011,Berserk ATK(60) Wave 1,100,150,Self,ATK,Percentage,60,icon_buff_plus_attack
503012,503012,Berserk DEF(-100) Wave 1,100,150,Self,DEF,Percentage,-100,icon_buff_minus_defense
503021,503011,Berserk ATK(80) Wave 2,100,150,Self,ATK,Percentage,80,icon_buff_plus_attack
503022,503012,Berserk DEF(-100) Wave 2,100,150,Self,DEF,Percentage,-100,icon_buff_minus_defense
503031,503011,Berserk ATK(100) Wave 3,100,150,Self,ATK,Percentage,100,icon_buff_plus_attack
503032,503012,Berserk DEF(-100) Wave 3,100,150,Self,DEF,Percentage,-100,icon_buff_minus_defense
503041,503011,Berserk ATK(120) Wave 4,100,150,Self,ATK,Percentage,120,icon_buff_plus_attack
503042,503012,Berserk DEF(-100) Wave 4,100,150,Self,DEF,Percentage,-100,icon_buff_minus_defense
503051,503011,Berserk ATK(150) Wave 5,100,150,Self,ATK,Percentage,150,icon_buff_plus_attack
503052,503012,Berserk DEF(-100) Wave 5,100,150,Self,DEF,Percentage,-100,icon_buff_minus_defense
503015,503015,Berserk CRI(1500),100,150,Self,CRI,Percentage,1500,icon_buff_plus_critical";

            var keys = new List<string>
            {
                nameof(SkillActionBuffSheet),
                nameof(ActionBuffSheet),
                nameof(StatBuffSheet),
            };
            foreach (var (key, value) in _sheets)
            {
                if (keys.Contains(key))
                {
                    _state = _state.SetState(Addresses.TableSheet.Derive(key), null!);
                }
            }

            _state = _state.SetState(Addresses.TableSheet.Derive(nameof(BuffSheet)), csv.Serialize());

            int championshipId = 1;
            int round = 1;
            Assert.True(_state.GetSheet<ArenaSheet>().TryGetValue(
                championshipId,
                out var row));

            Assert.True(row.TryGetRound(1, out var roundData));

            var random = new TestRandom(1);
            _state = JoinArena(_agent1Address, _avatar1Address, roundData.StartBlockIndex, championshipId, round, random);
            _state = JoinArena(_agent2Address, _avatar2Address, roundData.StartBlockIndex, championshipId, round, random);

            var arenaInfoAdr = ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!_state.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            foreach (var key in keys)
            {
                Assert.Null(_state.GetState(Addresses.GetSheetAddress(key)));
            }

            var action = new BattleArena4()
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            var myScoreAdr = ArenaScore.DeriveAddress(_avatar1Address, championshipId, round);
            var enemyScoreAdr = ArenaScore.DeriveAddress(_avatar2Address, championshipId, round);
            if (!_state.TryGetArenaScore(myScoreAdr, out var beforeMyScore))
            {
                throw new ArenaScoreNotFoundException($"myScoreAdr : {myScoreAdr}");
            }

            if (!_state.TryGetArenaScore(enemyScoreAdr, out var beforeEnemyScore))
            {
                throw new ArenaScoreNotFoundException($"enemyScoreAdr : {enemyScoreAdr}");
            }

            Assert.Empty(_avatar1.inventory.Materials);

            var gameConfigState = SetArenaInterval(2);
            _state = _state.SetState(GameConfigState.Address, gameConfigState.Serialize());

            var blockIndex = roundData.StartBlockIndex + 1;
            _state = action.Execute(new ActionContext
            {
                PreviousStates = _state,
                Signer = _agent1Address,
                Random = random,
                Rehearsal = false,
                BlockIndex = blockIndex,
            });

            if (!_state.TryGetArenaScore(myScoreAdr, out var myAfterScore))
            {
                throw new ArenaScoreNotFoundException($"myScoreAdr : {myScoreAdr}");
            }

            if (!_state.TryGetArenaScore(enemyScoreAdr, out var enemyAfterScore))
            {
                throw new ArenaScoreNotFoundException($"enemyScoreAdr : {enemyScoreAdr}");
            }

            if (!_state.TryGetArenaInformation(arenaInfoAdr, out var afterInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            var (myWinScore, myDefeatScore, enemyWinScore) =
                ArenaHelper.GetScores(beforeMyScore.Score, beforeEnemyScore.Score);

            var addMyScore = (afterInfo.Win * myWinScore) + (afterInfo.Lose * myDefeatScore);
            var addEnemyScore = afterInfo.Win * enemyWinScore;
            var expectedMyScore = Math.Max(beforeMyScore.Score + addMyScore, ArenaScore.ArenaScoreDefault);
            var expectedEnemyScore = Math.Max(beforeEnemyScore.Score + addEnemyScore, ArenaScore.ArenaScoreDefault);

            Assert.Equal(expectedMyScore, myAfterScore.Score);
            Assert.Equal(expectedEnemyScore, enemyAfterScore.Score);
            Assert.Equal(0, beforeInfo.Win);
            Assert.Equal(0, beforeInfo.Lose);

            var balance = _state.GetBalance(_agent1Address, _state.GetGoldCurrency());
            Assert.Equal(0, balance.RawValue);

            var avatarState = _state.GetAvatarStateV2(_avatar1Address);
            var medalCount = 0;
            if (roundData.ArenaType != ArenaType.OffSeason)
            {
                var medalId = ArenaHelper.GetMedalItemId(championshipId, round);
                avatarState.inventory.TryGetItem(medalId, out var medal);
                if (afterInfo.Win > 0)
                {
                    Assert.Equal(afterInfo.Win, medal.count);
                }
                else
                {
                    Assert.Null(medal);
                }

                medalCount = medal?.count ?? 0;
            }

            var materialCount = avatarState.inventory.Materials.Count();
            var high = ArenaHelper.GetRewardCount(beforeMyScore.Score) * 1 + medalCount;
            Assert.InRange(materialCount, 0, high);
        }
    }
}
