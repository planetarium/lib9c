namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Arena;
    using Nekoyume.Model;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Exceptions;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class BattleArena12Test
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
        private readonly Currency _crystal;
        private readonly Currency _ncg;
        private IWorld _initialStates;

        public BattleArena12Test(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialStates = new MockWorld();

            _sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in _sheets)
            {
                _initialStates = LegacyModule.SetState(
                    _initialStates,
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            _tableSheets = new TableSheets(_sheets);
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _crystal = Currency.Legacy("CRYSTAL", 18, null);
            _ncg = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
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
            _avatar1Address = avatar1State.address;

            // account 2
            var (agent2State, avatar2State) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                rankingMapAddress,
                clearStageId);
            _agent2Address = agent2State.address;
            _avatar2Address = avatar2State.address;

            // account 3
            var (agent3State, avatar3State) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                rankingMapAddress,
                1);
            _agent3Address = agent3State.address;
            _avatar3Address = avatar3State.address;

            // account 4
            var (agent4State, avatar4State) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                rankingMapAddress,
                1);

            _agent4Address = agent4State.address;
            _avatar4Address = avatar4State.address;

            _initialStates = LegacyModule.SetState(_initialStates, Addresses.GoldCurrency, goldCurrencyState.Serialize());
            _initialStates = AgentModule.SetAgentState(_initialStates, _agent1Address, agent1State);
            _initialStates = LegacyModule.SetState(
                _initialStates,
                _avatar1Address.Derive(LegacyInventoryKey),
                avatar1State.inventory.Serialize());
            _initialStates = LegacyModule.SetState(
                _initialStates,
                _avatar1Address.Derive(LegacyWorldInformationKey),
                avatar1State.worldInformation.Serialize());
            _initialStates = LegacyModule.SetState(
                _initialStates,
                _avatar1Address.Derive(LegacyQuestListKey),
                avatar1State.questList.Serialize());
            _initialStates = AvatarModule.SetAvatarStateV2(_initialStates, _avatar1Address, avatar1State);
            _initialStates = AgentModule.SetAgentState(_initialStates, _agent2Address, agent2State);
            _initialStates = AvatarModule.SetAvatarState(_initialStates, _avatar2Address, avatar2State);
            _initialStates = AgentModule.SetAgentState(_initialStates, _agent3Address, agent3State);
            _initialStates = AvatarModule.SetAvatarState(_initialStates, _avatar3Address, avatar3State);
            _initialStates = AgentModule.SetAgentState(_initialStates, _agent4Address, agent4State);
            _initialStates = LegacyModule.SetState(
                _initialStates,
                _avatar4Address.Derive(LegacyInventoryKey),
                avatar4State.inventory.Serialize());
            _initialStates = LegacyModule.SetState(
                _initialStates,
                _avatar4Address.Derive(LegacyWorldInformationKey),
                avatar4State.worldInformation.Serialize());
            _initialStates = LegacyModule.SetState(
                _initialStates,
                _avatar4Address.Derive(LegacyQuestListKey),
                avatar4State.questList.Serialize());
            _initialStates = AvatarModule.SetAvatarStateV2(_initialStates, _avatar4Address, avatar4State);
            _initialStates = LegacyModule.SetState(
                _initialStates,
                Addresses.GameConfig,
                new GameConfigState(_sheets[nameof(GameConfigSheet)]).Serialize());

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
        }

        [Theory]
        [InlineData(1, 1, false, 1, 5, 3)]
        [InlineData(1, 1, false, 1, 5, 4)]
        [InlineData(1, 1, false, 5, 5, 3)]
        [InlineData(1, 1, true, 1, 5, 3)]
        [InlineData(1, 1, false, 3, 5, 4)]
        [InlineData(1, 2, false, 1, 5, 3)]
        [InlineData(1, 2, true, 1, 5, 3)]
        [InlineData(1, 3, false, 1, int.MaxValue, 3)]
        [InlineData(1, 3, true, 1, int.MaxValue, 3)]
        public void Execute_Success(
            int championshipId,
            int round,
            bool isPurchased,
            int ticket,
            int arenaInterval,
            int randomSeed)
        {
            Execute(
                championshipId,
                round,
                isPurchased,
                ticket,
                arenaInterval,
                randomSeed,
                _agent1Address,
                _avatar1Address,
                _agent2Address,
                _avatar2Address);
        }

        [Fact]
        public void Execute_Backward_Compatibility_Success()
        {
            Execute(
                1,
                2,
                default,
                1,
                2,
                default,
                _agent2Address,
                _avatar2Address,
                _agent1Address,
                _avatar1Address);
        }

        [Fact]
        public void Execute_InvalidAddressException()
        {
            var action = new BattleArena
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar1Address,
                championshipId = 1,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            Assert.Throws<InvalidAddressException>(() => action.Execute(new ActionContext
            {
                PreviousState = new MockWorld(_initialStates),
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_FailedLoadStateException()
        {
            var action = new BattleArena
            {
                myAvatarAddress = _avatar2Address,
                enemyAvatarAddress = _avatar1Address,
                championshipId = 1,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext
            {
                PreviousState = new MockWorld(_initialStates),
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_NotEnoughClearedStageLevelException()
        {
            var action = new BattleArena
            {
                myAvatarAddress = _avatar4Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = 1,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() =>
                action.Execute(new ActionContext
                {
                    PreviousState = new MockWorld(_initialStates),
                    Signer = _agent4Address,
                    Random = new TestRandom(),
                    BlockIndex = 1,
                }));
        }

        [Fact]
        public void Execute_SheetRowNotFoundException()
        {
            var action = new BattleArena
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = 9999999,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext
            {
                PreviousState = new MockWorld(_initialStates),
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_ThisArenaIsClosedException()
        {
            var action = new BattleArena
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = 1,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            Assert.Throws<ThisArenaIsClosedException>(() => action.Execute(new ActionContext
            {
                PreviousState = new MockWorld(_initialStates),
                Signer = _agent1Address,
                Random = new TestRandom(),
                BlockIndex = 4480001,
            }));
        }

        [Fact]
        public void Execute_ArenaParticipantsNotFoundException()
        {
            var action = new BattleArena
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = 1,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            Assert.Throws<ArenaParticipantsNotFoundException>(() => action.Execute(new ActionContext
            {
                PreviousState = new MockWorld(_initialStates),
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
            const int championshipId = 1;
            const int round = 1;
            var context = new ActionContext();
            IWorld previousState = _initialStates;
            Assert.True(LegacyModule.GetSheet<ArenaSheet>(previousState).TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousState = excludeMe
                ? JoinArena(
                    context,
                    previousState,
                    _agent2Address,
                    _avatar2Address,
                    roundData.StartBlockIndex,
                    championshipId,
                    round,
                    random)
                : JoinArena(
                    context,
                    previousState,
                    _agent1Address,
                    _avatar1Address,
                    roundData.StartBlockIndex,
                    championshipId,
                    round,
                    random);

            var action = new BattleArena
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            Assert.Throws<AddressNotFoundInArenaParticipantsException>(() =>
                action.Execute(new ActionContext
                {
                    PreviousState = previousState,
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
            const int championshipId = 1;
            const int round = 2;
            var context = new ActionContext();
            var previousState = _initialStates;
            Assert.True(LegacyModule.GetSheet<ArenaSheet>(previousState).TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousState = JoinArena(
                context,
                previousState,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousState = JoinArena(
                context,
                previousState,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaScoreAdr = ArenaScore.DeriveAddress(
                isSigner
                    ? _avatar1Address
                    : _avatar2Address, roundData.ChampionshipId,
                roundData.Round);
            LegacyModule.TryGetArenaScore(previousState, arenaScoreAdr, out var arenaScore);
            arenaScore.AddScore(900);
            previousState = LegacyModule.SetState(previousState, arenaScoreAdr, arenaScore.Serialize());

            var action = new BattleArena
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;
            Assert.Throws<ValidateScoreDifferenceException>(() => action.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = previousState,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_InsufficientBalanceException()
        {
            const int championshipId = 1;
            const int round = 2;
            var context = new ActionContext();
            var previousState = _initialStates;
            Assert.True(LegacyModule.GetSheet<ArenaSheet>(previousState).TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousState = JoinArena(
                context,
                previousState,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousState = JoinArena(
                context,
                previousState,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!LegacyModule.TryGetArenaInformation(previousState, arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            beforeInfo.UseTicket(beforeInfo.Ticket);
            previousState = LegacyModule.SetState(previousState, arenaInfoAdr, beforeInfo.Serialize());

            var action = new BattleArena
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;
            Assert.Throws<InsufficientBalanceException>(() => action.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = previousState,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_ExceedPlayCountException()
        {
            const int championshipId = 1;
            const int round = 2;
            var context = new ActionContext();
            var previousState = _initialStates;
            Assert.True(LegacyModule.GetSheet<ArenaSheet>(previousState).TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousState = JoinArena(
                context,
                previousState,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousState = JoinArena(
                context,
                previousState,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!LegacyModule.TryGetArenaInformation(previousState, arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            var action = new BattleArena
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 2,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;
            Assert.Throws<ExceedPlayCountException>(() => action.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = previousState,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_ExceedTicketPurchaseLimitException()
        {
            const int championshipId = 1;
            const int round = 2;
            var context = new ActionContext();
            var previousState = _initialStates;
            Assert.True(LegacyModule.GetSheet<ArenaSheet>(previousState).TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousState = JoinArena(
                context,
                previousState,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousState = JoinArena(
                context,
                previousState,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!LegacyModule.TryGetArenaInformation(previousState, arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            beforeInfo.UseTicket(ArenaInformation.MaxTicketCount);
            var max = roundData.MaxPurchaseCount;
            for (var i = 0; i < max; i++)
            {
                beforeInfo.BuyTicket(roundData.MaxPurchaseCount);
            }

            previousState = LegacyModule.SetState(previousState, arenaInfoAdr, beforeInfo.Serialize());
            var price = ArenaHelper.GetTicketPrice(
                roundData,
                beforeInfo,
                LegacyModule.GetGoldCurrency(previousState));
            previousState = LegacyModule.MintAsset(previousState, context, _agent1Address, price);

            var action = new BattleArena
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;
            Assert.Throws<ExceedTicketPurchaseLimitException>(() => action.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = previousState,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_ExceedTicketPurchaseLimitDuringIntervalException()
        {
            const int championshipId = 1;
            const int round = 2;
            var context = new ActionContext();
            var previousState = _initialStates;
            Assert.True(LegacyModule.GetSheet<ArenaSheet>(previousState).TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousState = JoinArena(
                context,
                previousState,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousState = JoinArena(
                context,
                previousState,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!LegacyModule.TryGetArenaInformation(previousState, arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            beforeInfo.UseTicket(ArenaInformation.MaxTicketCount);
            var max = roundData.MaxPurchaseCountWithInterval;
            for (var i = 0; i < max; i++)
            {
                beforeInfo.BuyTicket(roundData.MaxPurchaseCount);
            }

            var purchasedCountDuringInterval = arenaInfoAdr.Derive(BattleArena.PurchasedCountKey);
            previousState = LegacyModule.SetState(previousState, arenaInfoAdr, beforeInfo.Serialize());
            previousState = LegacyModule.SetState(
                previousState,
                purchasedCountDuringInterval,
                new Integer(beforeInfo.PurchasedTicketCount));
            var price = ArenaHelper.GetTicketPrice(
                roundData,
                beforeInfo,
                LegacyModule.GetGoldCurrency(previousState));
            previousState = LegacyModule.MintAsset(previousState, context, _agent1Address, price);

            var action = new BattleArena
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;
            Assert.Throws<ExceedTicketPurchaseLimitDuringIntervalException>(() => action.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = previousState,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_CoolDownBlockException()
        {
            const int championshipId = 1;
            const int round = 2;
            var context = new ActionContext();
            var previousState = _initialStates;
            Assert.True(LegacyModule.GetSheet<ArenaSheet>(previousState).TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousState = JoinArena(
                context,
                previousState,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousState = JoinArena(
                context,
                previousState,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!LegacyModule.TryGetArenaInformation(previousState, arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            beforeInfo.UseTicket(ArenaInformation.MaxTicketCount);
            var max = roundData.MaxPurchaseCountWithInterval;
            previousState = LegacyModule.SetState(previousState, arenaInfoAdr, beforeInfo.Serialize());
            for (var i = 0; i < max; i++)
            {
                var price = ArenaHelper.GetTicketPrice(
                    roundData,
                    beforeInfo,
                    LegacyModule.GetGoldCurrency(previousState));
                previousState = LegacyModule.MintAsset(previousState, context, _agent1Address, price);
                beforeInfo.BuyTicket(roundData.MaxPurchaseCount);
            }

            var action = new BattleArena
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;

            var nextStates = action.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = previousState,
                Signer = _agent1Address,
                Random = new TestRandom(),
            });

            Assert.Throws<CoolDownBlockException>(() => action.Execute(new ActionContext
            {
                BlockIndex = blockIndex + 1,
                PreviousState = nextStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Theory]
        [InlineData(0, 30001, 1, 30001, typeof(DuplicatedRuneIdException))]
        [InlineData(1, 10002, 1, 30001, typeof(DuplicatedRuneSlotIndexException))]
        public void ExecuteDuplicatedException(int slotIndex, int runeId, int slotIndex2, int runeId2, Type exception)
        {
            long nextBlockIndex = 4;
            int championshipId = 1;
            int round = 1;
            int ticket = 1;
            int arenaInterval = 5;
            int randomSeed = 3;

            var context = new ActionContext();
            var previousState = _initialStates;
            Assert.True(LegacyModule.GetSheet<ArenaSheet>(previousState).TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom(randomSeed);
            previousState = JoinArena(
                context,
                previousState,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousState = JoinArena(
                context,
                previousState,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!LegacyModule.TryGetArenaInformation(previousState, arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            var ncgCurrency = LegacyModule.GetGoldCurrency(previousState);
            previousState = LegacyModule.MintAsset(previousState, context, _agent1Address, 99999 * ncgCurrency);

            var unlockRuneSlot = new UnlockRuneSlot()
            {
                AvatarAddress = _avatar1Address,
                SlotIndex = 1,
            };

            previousState = unlockRuneSlot.Execute(
                new ActionContext
                {
                    BlockIndex = 1,
                    PreviousState = previousState,
                    Signer = _agent1Address,
                    Random = new TestRandom(),
                });

            var action = new BattleArena
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = ticket,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>()
                {
                    new RuneSlotInfo(slotIndex, runeId),
                    new RuneSlotInfo(slotIndex2, runeId2),
                },
            };

            var myScoreAdr = ArenaScore.DeriveAddress(
                _avatar1Address,
                championshipId,
                round);
            var enemyScoreAdr = ArenaScore.DeriveAddress(
                _avatar2Address,
                championshipId,
                round);
            if (!LegacyModule.TryGetArenaScore(previousState, myScoreAdr, out var beforeMyScore))
            {
                throw new ArenaScoreNotFoundException($"myScoreAdr : {myScoreAdr}");
            }

            if (!LegacyModule.TryGetArenaScore(previousState, enemyScoreAdr, out var beforeEnemyScore))
            {
                throw new ArenaScoreNotFoundException($"enemyScoreAdr : {enemyScoreAdr}");
            }

            Assert.True(AvatarModule.TryGetAvatarStateV2(
                previousState,
                _agent1Address,
                _avatar1Address,
                out var previousMyAvatarState,
                out _));
            Assert.Empty(previousMyAvatarState.inventory.Materials);

            var gameConfigState = SetArenaInterval(arenaInterval);
            previousState = LegacyModule.SetState(previousState, GameConfigState.Address, gameConfigState.Serialize());

            var blockIndex = roundData.StartBlockIndex + nextBlockIndex;

            Assert.Throws(exception, () => action.Execute(new ActionContext
            {
                PreviousState = previousState,
                Signer = _agent1Address,
                Random = random,
                Rehearsal = false,
                BlockIndex = blockIndex,
            }));
        }

        [Fact]
        public void Execute_ValidateDuplicateTicketPurchaseException()
        {
            const int championshipId = 1;
            const int round = 1;
            var context = new ActionContext();
            var previousState = _initialStates;
            Assert.True(LegacyModule.GetSheet<ArenaSheet>(previousState).TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            if (roundData.ArenaType != ArenaType.OffSeason)
            {
                throw new InvalidSeasonException($"[{nameof(BattleArena)}] This test is only for OffSeason. ArenaType : {roundData.ArenaType}");
            }

            var random = new TestRandom();
            previousState = JoinArena(
                context,
                previousState,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousState = JoinArena(
                context,
                previousState,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!LegacyModule.TryGetArenaInformation(previousState, arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            beforeInfo.UseTicket(ArenaInformation.MaxTicketCount);

            var purchasedCountDuringInterval = arenaInfoAdr.Derive(BattleArena.PurchasedCountKey);
            previousState = LegacyModule.SetState(previousState, arenaInfoAdr, beforeInfo.Serialize());
            previousState = LegacyModule.SetState(
                previousState,
                purchasedCountDuringInterval,
                new Integer(beforeInfo.PurchasedTicketCount));
            var price = ArenaHelper.GetTicketPrice(
                roundData,
                beforeInfo,
                LegacyModule.GetGoldCurrency(previousState));
            previousState = LegacyModule.MintAsset(previousState, context, _agent1Address, price);

            var action = new BattleArena
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 8,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;
            Assert.Throws<TicketPurchaseLimitExceedException>(() => action.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = previousState,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        private static (AgentState AgentState, AvatarState AvatarState) GetAgentStateWithAvatarState(
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

        private void Execute(
            int championshipId,
            int round,
            bool isPurchased,
            int ticket,
            int arenaInterval,
            int randomSeed,
            Address myAgentAddress,
            Address myAvatarAddress,
            Address enemyAgentAddress,
            Address enemyAvatarAddress)
        {
            var context = new ActionContext();
            IWorld previousState = _initialStates;
            Assert.True(LegacyModule.GetSheet<ArenaSheet>(_initialStates).TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom(randomSeed);
            previousState = JoinArena(
                context,
                previousState,
                myAgentAddress,
                myAvatarAddress,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousState = JoinArena(
                context,
                previousState,
                enemyAgentAddress,
                enemyAvatarAddress,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(myAvatarAddress, championshipId, round);
            if (!LegacyModule.TryGetArenaInformation(previousState, arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            if (isPurchased)
            {
                beforeInfo.UseTicket(beforeInfo.Ticket);
                previousState = LegacyModule.SetState(previousState, arenaInfoAdr, beforeInfo.Serialize());
                for (var i = 0; i < ticket; i++)
                {
                    var price = ArenaHelper.GetTicketPrice(
                        roundData,
                        beforeInfo,
                        LegacyModule.GetGoldCurrency(previousState));
                    previousState = LegacyModule.MintAsset(previousState, context, myAgentAddress, price);
                    beforeInfo.BuyTicket(roundData.MaxPurchaseCount);
                }
            }

            var action = new BattleArena
            {
                myAvatarAddress = myAvatarAddress,
                enemyAvatarAddress = enemyAvatarAddress,
                championshipId = championshipId,
                round = round,
                ticket = ticket,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            var myScoreAdr = ArenaScore.DeriveAddress(
                myAvatarAddress,
                championshipId,
                round);
            var enemyScoreAdr = ArenaScore.DeriveAddress(
                enemyAvatarAddress,
                championshipId,
                round);
            if (!LegacyModule.TryGetArenaScore(previousState, myScoreAdr, out var beforeMyScore))
            {
                throw new ArenaScoreNotFoundException($"myScoreAdr : {myScoreAdr}");
            }

            if (!LegacyModule.TryGetArenaScore(previousState, enemyScoreAdr, out var beforeEnemyScore))
            {
                throw new ArenaScoreNotFoundException($"enemyScoreAdr : {enemyScoreAdr}");
            }

            Assert.True(AvatarModule.TryGetAvatarStateV2(
                previousState,
                myAgentAddress,
                myAvatarAddress,
                out var previousMyAvatarState,
                out _));
            Assert.Empty(previousMyAvatarState.inventory.Materials);

            var gameConfigState = SetArenaInterval(arenaInterval);
            previousState = LegacyModule.SetState(
                previousState,
                GameConfigState.Address,
                gameConfigState.Serialize());

            var blockIndex = roundData.StartBlockIndex < arenaInterval
                ? roundData.StartBlockIndex
                : roundData.StartBlockIndex + arenaInterval;
            var nextState = action.Execute(new ActionContext
            {
                PreviousState = previousState,
                Signer = myAgentAddress,
                Random = random,
                Rehearsal = false,
                BlockIndex = blockIndex,
            });

            if (!LegacyModule.TryGetArenaScore(nextState, myScoreAdr, out var myAfterScore))
            {
                throw new ArenaScoreNotFoundException($"myScoreAdr : {myScoreAdr}");
            }

            if (!LegacyModule.TryGetArenaScore(nextState, enemyScoreAdr, out var enemyAfterScore))
            {
                throw new ArenaScoreNotFoundException($"enemyScoreAdr : {enemyScoreAdr}");
            }

            if (!LegacyModule.TryGetArenaInformation(nextState, arenaInfoAdr, out var afterInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            var (myWinScore, myDefeatScore, enemyWinScore) =
                ArenaHelper.GetScoresV1(beforeMyScore.Score, beforeEnemyScore.Score);

            var addMyScore = afterInfo.Win * myWinScore + afterInfo.Lose * myDefeatScore;
            var addEnemyScore = afterInfo.Win * enemyWinScore;
            var expectedMyScore = Math.Max(
                beforeMyScore.Score + addMyScore,
                ArenaScore.ArenaScoreDefault);
            var expectedEnemyScore = Math.Max(
                beforeEnemyScore.Score + addEnemyScore,
                ArenaScore.ArenaScoreDefault);

            Assert.Equal(expectedMyScore, myAfterScore.Score);
            Assert.Equal(expectedEnemyScore, enemyAfterScore.Score);
            Assert.Equal(
                isPurchased
                    ? 0
                    : ArenaInformation.MaxTicketCount,
                beforeInfo.Ticket);
            Assert.Equal(0, beforeInfo.Win);
            Assert.Equal(0, beforeInfo.Lose);

            var useTicket = Math.Min(ticket, beforeInfo.Ticket);
            Assert.Equal(beforeInfo.Ticket - useTicket, afterInfo.Ticket);
            Assert.Equal(ticket, afterInfo.Win + afterInfo.Lose);

            var balance = LegacyModule.GetBalance(
                nextState,
                myAgentAddress,
                LegacyModule.GetGoldCurrency(nextState));
            if (isPurchased)
            {
                Assert.Equal(ticket, afterInfo.PurchasedTicketCount);
            }

            Assert.Equal(0, balance.RawValue);

            var avatarState = AvatarModule.GetAvatarStateV2(nextState, myAvatarAddress);
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

        private IWorld JoinArena(
            IActionContext context,
            IWorld state,
            Address signer,
            Address avatarAddress,
            long blockIndex,
            int championshipId,
            int round,
            IRandom random)
        {
            var preCurrency = 1000 * _crystal;
            state = LegacyModule.MintAsset(state, context, signer, preCurrency);

            var action = new JoinArena
            {
                championshipId = championshipId,
                round = round,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                avatarAddress = avatarAddress,
                runeInfos = new List<RuneSlotInfo>(),
            };

            return action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = signer,
                    Random = random,
                    Rehearsal = false,
                    BlockIndex = blockIndex,
                });
        }

        private GameConfigState SetArenaInterval(int interval)
        {
            var gameConfigState = LegacyModule.GetGameConfigState(_initialStates);
            var sheet = _tableSheets.GameConfigSheet;
            foreach (var value in sheet.Values)
            {
                if (value.Key.Equals("battle_arena_interval"))
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
    }
}
