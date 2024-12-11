namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Arena;
    using Nekoyume.Exceptions;
    using Nekoyume.Model;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class BattleArenaTest
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

        public BattleArenaTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialStates = new World(MockUtil.MockModernWorldState);

            _sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in _sheets)
            {
                _initialStates = _initialStates.SetLegacyState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            _tableSheets = new TableSheets(_sheets);
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1419
            _crystal = Currency.Legacy("CRYSTAL", 18, null);
            _ncg = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(_ncg);

            var rankingMapAddress = new PrivateKey().Address;
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

            _initialStates = _initialStates
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
                .SetAgentState(_agent1Address, agent1State)
                .SetAvatarState(_avatar1Address, avatar1State)
                .SetAgentState(_agent2Address, agent2State)
                .SetAvatarState(_avatar2Address, avatar2State)
                .SetAgentState(_agent3Address, agent3State)
                .SetAvatarState(_avatar3Address, avatar3State)
                .SetAgentState(_agent4Address, agent4State)
                .SetAvatarState(_avatar4Address, avatar4State)
                .SetLegacyState(
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

            Assert.Throws<InvalidAddressException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = _initialStates,
                        Signer = _agent1Address,
                        RandomSeed = 0,
                    }));
        }

        [Fact]
        public void Execute_MedalIdNotFoundException()
        {
            var prevState = _initialStates;
            var context = new ActionContext
            {
                PreviousState = prevState,
                Signer = _agent1Address,
                RandomSeed = 0,
            };
            prevState = prevState.SetLegacyState(
                Addresses.TableSheet.Derive("ArenaSheet"),
                @"id,round,arena_type,start_block_index,end_block_index,required_medal_count,entrance_fee,ticket_price,additional_ticket_price,max_purchase_count,max_purchase_count_during_interval,medal_id
1,1,Season,1,100,0,0,5,2,80,79".Serialize());
            prevState = JoinArena(
                context,
                prevState,
                _agent1Address,
                _avatar1Address,
                1,
                1,
                1,
                new TestRandom());
            prevState = JoinArena(
                context,
                prevState,
                _agent2Address,
                _avatar2Address,
                1,
                1,
                1,
                new TestRandom());

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
            Assert.Throws<MedalIdNotFoundException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = prevState,
                        Signer = _agent1Address,
                        RandomSeed = 0,
                        BlockIndex = 10,
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

            Assert.Throws<FailedLoadStateException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = _initialStates,
                        Signer = _agent1Address,
                        RandomSeed = 0,
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

            Assert.Throws<SheetRowNotFoundException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = _initialStates,
                        Signer = _agent1Address,
                        RandomSeed = 0,
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

            Assert.Throws<ThisArenaIsClosedException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = _initialStates,
                        Signer = _agent1Address,
                        RandomSeed = 0,
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

            Assert.Throws<ArenaParticipantsNotFoundException>(
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = _initialStates,
                        Signer = _agent1Address,
                        RandomSeed = 0,
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
            var previousStates = _initialStates;
            Assert.True(
                previousStates.GetSheet<ArenaSheet>().TryGetValue(
                    championshipId,
                    out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousStates = excludeMe
                ? JoinArena(
                    context,
                    previousStates,
                    _agent2Address,
                    _avatar2Address,
                    roundData.StartBlockIndex,
                    championshipId,
                    round,
                    random)
                : JoinArena(
                    context,
                    previousStates,
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

            Assert.Throws<AddressNotFoundInArenaParticipantsException>(
                () =>
                    action.Execute(
                        new ActionContext
                        {
                            PreviousState = previousStates,
                            Signer = _agent1Address,
                            RandomSeed = 0,
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
            var previousStates = _initialStates;
            Assert.True(
                previousStates.GetSheet<ArenaSheet>().TryGetValue(
                    championshipId,
                    out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousStates = JoinArena(
                context,
                previousStates,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousStates = JoinArena(
                context,
                previousStates,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaScoreAdr = ArenaScore.DeriveAddress(
                isSigner
                    ? _avatar1Address
                    : _avatar2Address,
                roundData.ChampionshipId,
                roundData.Round);
            previousStates.TryGetArenaScore(arenaScoreAdr, out var arenaScore);
            arenaScore.AddScore(900);
            previousStates = previousStates.SetLegacyState(arenaScoreAdr, arenaScore.Serialize());

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
            Assert.Throws<ValidateScoreDifferenceException>(
                () => action.Execute(
                    new ActionContext
                    {
                        BlockIndex = blockIndex,
                        PreviousState = previousStates,
                        Signer = _agent1Address,
                        RandomSeed = 0,
                    }));
        }

        [Fact]
        public void Execute_InsufficientBalanceException()
        {
            const int championshipId = 1;
            const int round = 2;
            var context = new ActionContext();
            var previousStates = _initialStates;
            Assert.True(
                previousStates.GetSheet<ArenaSheet>().TryGetValue(
                    championshipId,
                    out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousStates = JoinArena(
                context,
                previousStates,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousStates = JoinArena(
                context,
                previousStates,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!previousStates.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            beforeInfo.UseTicket(beforeInfo.Ticket);
            previousStates = previousStates.SetLegacyState(arenaInfoAdr, beforeInfo.Serialize());

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
            Assert.Throws<InsufficientBalanceException>(
                () => action.Execute(
                    new ActionContext
                    {
                        BlockIndex = blockIndex,
                        PreviousState = previousStates,
                        Signer = _agent1Address,
                        RandomSeed = 0,
                    }));
        }

        [Fact]
        public void Execute_ExceedPlayCountException()
        {
            const int championshipId = 1;
            const int round = 2;
            var context = new ActionContext();
            var previousStates = _initialStates;
            Assert.True(
                previousStates.GetSheet<ArenaSheet>().TryGetValue(
                    championshipId,
                    out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousStates = JoinArena(
                context,
                previousStates,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousStates = JoinArena(
                context,
                previousStates,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!previousStates.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
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
            Assert.Throws<ExceedPlayCountException>(
                () => action.Execute(
                    new ActionContext
                    {
                        BlockIndex = blockIndex,
                        PreviousState = previousStates,
                        Signer = _agent1Address,
                        RandomSeed = 0,
                    }));
        }

        [Fact]
        public void Execute_ExceedTicketPurchaseLimitException()
        {
            const int championshipId = 1;
            const int round = 2;
            var context = new ActionContext();
            var previousStates = _initialStates;
            Assert.True(
                previousStates.GetSheet<ArenaSheet>().TryGetValue(
                    championshipId,
                    out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousStates = JoinArena(
                context,
                previousStates,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousStates = JoinArena(
                context,
                previousStates,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!previousStates.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            beforeInfo.UseTicket(ArenaInformation.MaxTicketCount);
            var max = roundData.MaxPurchaseCount;
            for (var i = 0; i < max; i++)
            {
                beforeInfo.BuyTicket(roundData.MaxPurchaseCount);
            }

            previousStates = previousStates.SetLegacyState(arenaInfoAdr, beforeInfo.Serialize());
            var price = ArenaHelper.GetTicketPrice(
                roundData,
                beforeInfo,
                previousStates.GetGoldCurrency());
            previousStates = previousStates.MintAsset(context, _agent1Address, price);

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
            Assert.Throws<ExceedTicketPurchaseLimitException>(
                () => action.Execute(
                    new ActionContext
                    {
                        BlockIndex = blockIndex,
                        PreviousState = previousStates,
                        Signer = _agent1Address,
                        RandomSeed = 0,
                    }));
        }

        [Fact]
        public void Execute_ExceedTicketPurchaseLimitDuringIntervalException()
        {
            const int championshipId = 1;
            const int round = 2;
            var context = new ActionContext();
            var previousStates = _initialStates;
            Assert.True(
                previousStates.GetSheet<ArenaSheet>().TryGetValue(
                    championshipId,
                    out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousStates = JoinArena(
                context,
                previousStates,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousStates = JoinArena(
                context,
                previousStates,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!previousStates.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
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
            previousStates = previousStates
                .SetLegacyState(arenaInfoAdr, beforeInfo.Serialize())
                .SetLegacyState(
                    purchasedCountDuringInterval,
                    new Integer(beforeInfo.PurchasedTicketCount));
            var price = ArenaHelper.GetTicketPrice(
                roundData,
                beforeInfo,
                previousStates.GetGoldCurrency());
            previousStates = previousStates.MintAsset(context, _agent1Address, price);

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
            Assert.Throws<ExceedTicketPurchaseLimitDuringIntervalException>(
                () => action.Execute(
                    new ActionContext
                    {
                        BlockIndex = blockIndex,
                        PreviousState = previousStates,
                        Signer = _agent1Address,
                        RandomSeed = 0,
                    }));
        }

        [Fact]
        public void Execute_CoolDownBlockException()
        {
            const int championshipId = 1;
            const int round = 2;
            var context = new ActionContext();
            var previousStates = _initialStates;
            Assert.True(
                previousStates.GetSheet<ArenaSheet>().TryGetValue(
                    championshipId,
                    out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousStates = JoinArena(
                context,
                previousStates,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousStates = JoinArena(
                context,
                previousStates,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!previousStates.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            beforeInfo.UseTicket(ArenaInformation.MaxTicketCount);
            var max = roundData.MaxPurchaseCountWithInterval;
            previousStates = previousStates.SetLegacyState(arenaInfoAdr, beforeInfo.Serialize());
            for (var i = 0; i < max; i++)
            {
                var price = ArenaHelper.GetTicketPrice(
                    roundData,
                    beforeInfo,
                    previousStates.GetGoldCurrency());
                previousStates = previousStates.MintAsset(context, _agent1Address, price);
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

            var nextStates = action.Execute(
                new ActionContext
                {
                    BlockIndex = blockIndex,
                    PreviousState = previousStates,
                    Signer = _agent1Address,
                    RandomSeed = 0,
                });

            Assert.Throws<CoolDownBlockException>(
                () => action.Execute(
                    new ActionContext
                    {
                        BlockIndex = blockIndex + 1,
                        PreviousState = nextStates,
                        Signer = _agent1Address,
                        RandomSeed = 0,
                    }));
        }

        [Theory]
        [InlineData(0, 30001, 1, 30001, typeof(DuplicatedRuneIdException))]
        [InlineData(1, 10002, 1, 30001, typeof(DuplicatedRuneSlotIndexException))]
        public void ExecuteDuplicatedException(int slotIndex, int runeId, int slotIndex2, int runeId2, Type exception)
        {
            long nextBlockIndex = 4;
            var championshipId = 1;
            var round = 1;
            var ticket = 1;
            var arenaInterval = 5;
            var randomSeed = 3;

            var context = new ActionContext();
            var previousStates = _initialStates;
            Assert.True(
                _initialStates.GetSheet<ArenaSheet>().TryGetValue(
                    championshipId,
                    out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom(randomSeed);
            previousStates = JoinArena(
                context,
                previousStates,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousStates = JoinArena(
                context,
                previousStates,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!previousStates.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            var ncgCurrency = previousStates.GetGoldCurrency();
            previousStates = previousStates.MintAsset(context, _agent1Address, 99999 * ncgCurrency);

            var unlockRuneSlot = new UnlockRuneSlot()
            {
                AvatarAddress = _avatar1Address,
                SlotIndex = 1,
            };

            previousStates = unlockRuneSlot.Execute(
                new ActionContext
                {
                    BlockIndex = 1,
                    PreviousState = previousStates,
                    Signer = _agent1Address,
                    RandomSeed = 0,
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
                    new (slotIndex, runeId),
                    new (slotIndex2, runeId2),
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
            if (!previousStates.TryGetArenaScore(myScoreAdr, out var beforeMyScore))
            {
                throw new ArenaScoreNotFoundException($"myScoreAdr : {myScoreAdr}");
            }

            if (!previousStates.TryGetArenaScore(enemyScoreAdr, out var beforeEnemyScore))
            {
                throw new ArenaScoreNotFoundException($"enemyScoreAdr : {enemyScoreAdr}");
            }

            Assert.True(
                previousStates.TryGetAvatarState(
                    _agent1Address,
                    _avatar1Address,
                    out var previousMyAvatarState));
            Assert.Empty(previousMyAvatarState.inventory.Materials);

            var gameConfigState = SetArenaInterval(arenaInterval);
            previousStates = previousStates.SetLegacyState(GameConfigState.Address, gameConfigState.Serialize());

            var blockIndex = roundData.StartBlockIndex + nextBlockIndex;

            Assert.Throws(
                exception,
                () => action.Execute(
                    new ActionContext
                    {
                        PreviousState = previousStates,
                        Signer = _agent1Address,
                        RandomSeed = random.Seed,
                        BlockIndex = blockIndex,
                    }));
        }

        [Fact]
        public void Execute_ValidateDuplicateTicketPurchaseException()
        {
            const int championshipId = 1;
            const int round = 1;
            var context = new ActionContext();
            var previousStates = _initialStates;
            Assert.True(
                previousStates.GetSheet<ArenaSheet>().TryGetValue(
                    championshipId,
                    out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            if (roundData.ArenaType != ArenaType.OffSeason)
            {
                throw new InvalidSeasonException(
                    $"[{nameof(BattleArena)}] This test is only for OffSeason. ArenaType : {roundData.ArenaType}");
            }

            var random = new TestRandom();
            previousStates = JoinArena(
                context,
                previousStates,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousStates = JoinArena(
                context,
                previousStates,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!previousStates.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            beforeInfo.UseTicket(ArenaInformation.MaxTicketCount);

            var purchasedCountDuringInterval = arenaInfoAdr.Derive(BattleArena.PurchasedCountKey);
            previousStates = previousStates
                .SetLegacyState(arenaInfoAdr, beforeInfo.Serialize())
                .SetLegacyState(
                    purchasedCountDuringInterval,
                    new Integer(beforeInfo.PurchasedTicketCount));
            var price = ArenaHelper.GetTicketPrice(
                roundData,
                beforeInfo,
                previousStates.GetGoldCurrency());
            previousStates = previousStates.MintAsset(context, _agent1Address, price);

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
            Assert.Throws<TicketPurchaseLimitExceedException>(
                () => action.Execute(
                    new ActionContext
                    {
                        BlockIndex = blockIndex,
                        PreviousState = previousStates,
                        Signer = _agent1Address,
                        RandomSeed = 0,
                    }));
        }

        [Fact]
        public void ExecuteRuneNotFoundException()
        {
            var previousStates = _initialStates;
            var context = new ActionContext();
            Assert.True(
                previousStates.GetSheet<ArenaSheet>().TryGetValue(
                    1,
                    out var row));

            if (!row.TryGetRound(1, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({1})");
            }

            if (roundData.ArenaType != ArenaType.OffSeason)
            {
                throw new InvalidSeasonException(
                    $"[{nameof(BattleArena)}] This test is only for OffSeason. ArenaType : {roundData.ArenaType}");
            }

            var random = new TestRandom();
            previousStates = JoinArena(
                context,
                previousStates,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                1,
                1,
                random);
            previousStates = JoinArena(
                context,
                previousStates,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                1,
                1,
                random);

            var action = new BattleArena
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = 1,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo> { new (0, 10035), },
            };
            Assert.Throws<RuneNotFoundException>(
                () => action.Execute(
                    new ActionContext
                    {
                        BlockIndex = roundData.StartBlockIndex + 1,
                        PreviousState = previousStates,
                        Signer = _agent1Address,
                        RandomSeed = 0,
                    }));
        }

        [Theory]
        [InlineData(8, null)]
        [InlineData(100, null)]
        [InlineData(0, typeof(ArgumentException))]
        [InlineData(-1, typeof(ArgumentException))]
        public void PlainValue(int ticket, Type exc)
        {
            var action = new BattleArena
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = 1,
                round = 1,
                ticket = ticket,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };
            var plainValue = action.PlainValue;
            var des = new BattleArena();
            if (exc is null)
            {
                des.LoadPlainValue(plainValue);
                Assert.Equal(plainValue, des.PlainValue);
            }
            else
            {
                Assert.Throws(exc, () => des.LoadPlainValue(plainValue));
            }
        }

        private static (AgentState AgentState, AvatarState AvatarState) GetAgentStateWithAvatarState(
            IReadOnlyDictionary<string, string> sheets,
            TableSheets tableSheets,
            Address rankingMapAddress,
            int clearStageId)
        {
            var agentAddress = new PrivateKey().Address;
            var agentState = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                rankingMapAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                tableSheets.WorldSheet,
                clearStageId);

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
            var previousStates = _initialStates;
            Assert.True(
                _initialStates.GetSheet<ArenaSheet>().TryGetValue(
                    championshipId,
                    out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom(randomSeed);
            previousStates = JoinArena(
                context,
                previousStates,
                myAgentAddress,
                myAvatarAddress,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousStates = JoinArena(
                context,
                previousStates,
                enemyAgentAddress,
                enemyAvatarAddress,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(myAvatarAddress, championshipId, round);
            if (!previousStates.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            if (isPurchased)
            {
                beforeInfo.UseTicket(beforeInfo.Ticket);
                previousStates = previousStates.SetLegacyState(arenaInfoAdr, beforeInfo.Serialize());
                for (var i = 0; i < ticket; i++)
                {
                    var price = ArenaHelper.GetTicketPrice(
                        roundData,
                        beforeInfo,
                        previousStates.GetGoldCurrency());
                    previousStates = previousStates.MintAsset(context, myAgentAddress, price);
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
            if (!previousStates.TryGetArenaScore(myScoreAdr, out var beforeMyScore))
            {
                throw new ArenaScoreNotFoundException($"myScoreAdr : {myScoreAdr}");
            }

            if (!previousStates.TryGetArenaScore(enemyScoreAdr, out var beforeEnemyScore))
            {
                throw new ArenaScoreNotFoundException($"enemyScoreAdr : {enemyScoreAdr}");
            }

            Assert.True(
                previousStates.TryGetAvatarState(
                    myAgentAddress,
                    myAvatarAddress,
                    out var previousMyAvatarState));
            Assert.Empty(previousMyAvatarState.inventory.Materials);

            var gameConfigState = SetArenaInterval(arenaInterval);
            previousStates = previousStates.SetLegacyState(GameConfigState.Address, gameConfigState.Serialize());

            var blockIndex = roundData.StartBlockIndex < arenaInterval
                ? roundData.StartBlockIndex
                : roundData.StartBlockIndex + arenaInterval;
            var nextStates = action.Execute(
                new ActionContext
                {
                    PreviousState = previousStates,
                    Signer = myAgentAddress,
                    RandomSeed = random.Seed,
                    BlockIndex = blockIndex,
                });

            if (!nextStates.TryGetArenaScore(myScoreAdr, out var myScoreNext))
            {
                throw new ArenaScoreNotFoundException($"myScoreAdr : {myScoreAdr}");
            }

            if (!nextStates.TryGetArenaScore(enemyScoreAdr, out var enemyScoreNext))
            {
                throw new ArenaScoreNotFoundException($"enemyScoreAdr : {enemyScoreAdr}");
            }

            if (!nextStates.TryGetArenaInformation(arenaInfoAdr, out var myAfterInfoNext))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            var (myWinScore, myDefeatScore, enemyDefeatScore) =
                ArenaHelper.GetScores(beforeMyScore.Score, beforeEnemyScore.Score);

            var addMyScore = myAfterInfoNext.Win * myWinScore + myAfterInfoNext.Lose * myDefeatScore;
            var addEnemyScore = myAfterInfoNext.Win * enemyDefeatScore;
            var expectedMyScore = Math.Max(
                beforeMyScore.Score + addMyScore,
                ArenaScore.ArenaScoreDefault);
            var expectedEnemyScore = Math.Max(
                beforeEnemyScore.Score + addEnemyScore,
                ArenaScore.ArenaScoreDefault);

            Assert.Equal(expectedMyScore, myScoreNext.Score);
            Assert.Equal(expectedEnemyScore, enemyScoreNext.Score);
            Assert.Equal(
                isPurchased
                    ? 0
                    : ArenaInformation.MaxTicketCount,
                beforeInfo.Ticket);
            Assert.Equal(0, beforeInfo.Win);
            Assert.Equal(0, beforeInfo.Lose);

            var useTicket = Math.Min(ticket, beforeInfo.Ticket);
            Assert.Equal(beforeInfo.Ticket - useTicket, myAfterInfoNext.Ticket);
            Assert.Equal(ticket, myAfterInfoNext.Win + myAfterInfoNext.Lose);

            var balance = nextStates.GetBalance(
                myAgentAddress,
                nextStates.GetGoldCurrency());
            if (isPurchased)
            {
                Assert.Equal(ticket, myAfterInfoNext.PurchasedTicketCount);
            }

            Assert.Equal(0, balance.RawValue);

            var myAvatarStateNext = nextStates.GetAvatarState(myAvatarAddress);
            var medalCount = 0;
            if (roundData.ArenaType != ArenaType.OffSeason)
            {
                var medalId = roundData.MedalId;
                myAvatarStateNext.inventory.TryGetItem(medalId, out var medal);
                if (myAfterInfoNext.Win > 0)
                {
                    Assert.Equal(myAfterInfoNext.Win, medal.count);
                }
                else
                {
                    Assert.Null(medal);
                }

                medalCount = medal?.count ?? 0;
            }

            var materialCount = myAvatarStateNext.inventory.Materials.Count();
            var high = ArenaHelper.GetRewardCount(beforeMyScore.Score) * ticket + medalCount;
            Assert.InRange(materialCount, 0, high);

            var myArenaAvatarStateAddr = ArenaAvatarState.DeriveAddress(myAvatarAddress);
            Assert.True(nextStates.TryGetArenaAvatarState(myArenaAvatarStateAddr, out var myArenaAvatarStateNext));

            // check my ArenaParticipant
            var arenaParticipantNext = nextStates.GetArenaParticipant(championshipId, round, myAvatarAddress);
            Assert.NotNull(arenaParticipantNext);
            Assert.Equal(myAvatarAddress, arenaParticipantNext.AvatarAddr);
            Assert.Equal(myAvatarStateNext.name, arenaParticipantNext.Name);
            Assert.Equal(myAvatarStateNext.GetPortraitId(), arenaParticipantNext.PortraitId);
            Assert.Equal(myAvatarStateNext.level, arenaParticipantNext.Level);
            // Assert.Equal(cp, arenaParticipantNext.Cp);
            Assert.Equal(myScoreNext.Score, arenaParticipantNext.Score);
            Assert.Equal(myAfterInfoNext.Ticket, arenaParticipantNext.Ticket);
            Assert.Equal(myAfterInfoNext.TicketResetCount, arenaParticipantNext.TicketResetCount);
            Assert.Equal(myAfterInfoNext.PurchasedTicketCount, arenaParticipantNext.PurchasedTicketCount);
            Assert.Equal(myAfterInfoNext.Win, arenaParticipantNext.Win);
            Assert.Equal(myAfterInfoNext.Lose, arenaParticipantNext.Lose);
            Assert.Equal(myArenaAvatarStateNext.LastBattleBlockIndex, arenaParticipantNext.LastBattleBlockIndex);

            // check enemy's ArenaParticipant
            var enemyArenaParticipantNext = nextStates.GetArenaParticipant(championshipId, round, enemyAvatarAddress);
            Assert.NotNull(enemyArenaParticipantNext);
            Assert.Equal(enemyScoreNext.Score, enemyArenaParticipantNext.Score);
        }

        private IWorld JoinArena(
            IActionContext context,
            IWorld states,
            Address signer,
            Address avatarAddress,
            long blockIndex,
            int championshipId,
            int round,
            IRandom random)
        {
            var preCurrency = 1000 * _crystal;
            states = states.MintAsset(context, signer, preCurrency);

            var action = new JoinArena
            {
                avatarAddress = avatarAddress,
                championshipId = championshipId,
                round = round,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            states = action.Execute(
                new ActionContext
                {
                    PreviousState = states,
                    Signer = signer,
                    RandomSeed = random.Seed,
                    BlockIndex = blockIndex,
                });
            return states;
        }

        private GameConfigState SetArenaInterval(int interval)
        {
            var gameConfigState = _initialStates.GetGameConfigState();
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
