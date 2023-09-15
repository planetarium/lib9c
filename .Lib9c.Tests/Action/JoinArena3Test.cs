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
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class JoinArena3Test
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;
        private readonly Address _signer;
        private readonly Address _signer2;
        private readonly Address _avatarAddress;
        private readonly Address _avatar2Address;
        private readonly IRandom _random;
        private readonly Currency _currency;
        private IWorld _state;

        public JoinArena3Test(ITestOutputHelper outputHelper)
        {
            _random = new TestRandom();
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _state = new MockWorld();

            _signer = new PrivateKey().ToAddress();
            _avatarAddress = _signer.Derive("avatar");
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var rankingMapAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_signer);
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                _avatarAddress,
                _signer,
                0,
                tableSheets.GetAvatarSheets(),
                gameConfigState,
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInRankingBoard),
            };
            agentState.avatarAddresses[0] = _avatarAddress;
            avatarState.level = GameConfig.RequireClearedStageLevel.ActionsInRankingBoard;

            _signer2 = new PrivateKey().ToAddress();
            _avatar2Address = _signer2.Derive("avatar");
            var agent2State = new AgentState(_signer2);

            var avatar2State = new AvatarState(
                _avatar2Address,
                _signer2,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    tableSheets.WorldSheet,
                    1),
            };
            agent2State.avatarAddresses[0] = _avatar2Address;
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(currency);
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("CRYSTAL", 18, null);
#pragma warning restore CS0618

            _state = AgentModule.SetAgentState(_state, _signer, agentState);
            _state = AvatarModule.SetAvatarState(
                _state,
                _avatarAddress,
                avatarState,
                true,
                true,
                true,
                true);
            _state = AgentModule.SetAgentState(_state, _signer2, agent2State);
            _state = AvatarModule.SetAvatarState(
                _state,
                _avatar2Address,
                avatar2State,
                true,
                true,
                true,
                true);
            _state = LegacyModule.SetState(
                _state,
                gameConfigState.address,
                gameConfigState.Serialize());
            _state = LegacyModule.SetState(
                _state,
                Addresses.GoldCurrency,
                goldCurrencyState.Serialize());

            foreach ((string key, string value) in sheets)
            {
                _state = LegacyModule.SetState(
                    _state,
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
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

        public AvatarState GetAvatarState(AvatarState avatarState, out List<Guid> equipments, out List<Guid> costumes)
        {
            avatarState.level = 999;
            (equipments, costumes) = GetDummyItems(avatarState);

            _state = AvatarModule.SetAvatarState(
                _state,
                _avatarAddress,
                avatarState,
                true,
                true,
                true,
                true);

            return avatarState;
        }

        public AvatarState AddMedal(AvatarState avatarState, ArenaSheet.Row row, int count = 1)
        {
            var materialSheet = LegacyModule.GetSheet<MaterialItemSheet>(_state);
            foreach (var data in row.Round)
            {
                if (!data.ArenaType.Equals(ArenaType.Season))
                {
                    continue;
                }

                var itemId = ArenaHelper.GetMedalItemId(data.ChampionshipId, data.Round);
                var material = ItemFactory.CreateMaterial(materialSheet, itemId);
                avatarState.inventory.AddItem(material, count);
            }

            _state = AvatarModule.SetAvatarState(
                _state,
                _avatarAddress,
                avatarState,
                true,
                true,
                true,
                true);

            return avatarState;
        }

        [Theory]
        [InlineData(0, 1, 1, "0")]
        [InlineData(4_479_999L, 1, 2, "998001")]
        [InlineData(4_480_001L, 1, 2, "998001")]
        [InlineData(100, 1, 8, "998001")]
        public void Execute(long blockIndex, int championshipId, int round, string balance)
        {
            var arenaSheet = LegacyModule.GetSheet<ArenaSheet>(_state);
            if (!arenaSheet.TryGetValue(championshipId, out var row))
            {
                throw new SheetRowNotFoundException(
                    nameof(ArenaSheet), $"championship Id : {championshipId}");
            }

            var avatarState = AvatarModule.GetAvatarState(_state, _avatarAddress);
            avatarState = GetAvatarState(avatarState, out var equipments, out var costumes);
            avatarState = AddMedal(avatarState, row, 80);

            var context = new ActionContext();
            var state = (balance == "0")
                ? _state
                : LegacyModule.MintAsset(_state, context, _signer, FungibleAssetValue.Parse(_currency, balance));

            var action = new JoinArena()
            {
                championshipId = championshipId,
                round = round,
                costumes = costumes,
                equipments = equipments,
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
            };

            state = action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _signer,
                    Random = _random,
                    Rehearsal = false,
                    BlockIndex = blockIndex,
                });

            // ArenaParticipants
            var arenaParticipantsAdr = ArenaParticipants.DeriveAddress(championshipId, round);
            var serializedArenaParticipants = (List)LegacyModule.GetState(state, arenaParticipantsAdr);
            var arenaParticipants = new ArenaParticipants(serializedArenaParticipants);

            Assert.Equal(arenaParticipantsAdr, arenaParticipants.Address);
            Assert.Equal(_avatarAddress, arenaParticipants.AvatarAddresses.First());

            // ArenaAvatarState
            var arenaAvatarStateAdr = ArenaAvatarState.DeriveAddress(_avatarAddress);
            var serializedArenaAvatarState = (List)LegacyModule.GetState(state, arenaAvatarStateAdr);
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

            // ArenaScore
            var arenaScoreAdr = ArenaScore.DeriveAddress(_avatarAddress, championshipId, round);
            var serializedArenaScore = (List)LegacyModule.GetState(state, arenaScoreAdr);
            var arenaScore = new ArenaScore(serializedArenaScore);

            Assert.Equal(arenaScoreAdr, arenaScore.Address);
            Assert.Equal(GameConfig.ArenaScoreDefault, arenaScore.Score);

            // ArenaInformation
            var arenaInformationAdr = ArenaInformation.DeriveAddress(_avatarAddress, championshipId, round);
            var serializedArenaInformation = (List)LegacyModule.GetState(state, arenaInformationAdr);
            var arenaInformation = new ArenaInformation(serializedArenaInformation);

            Assert.Equal(arenaInformationAdr, arenaInformation.Address);
            Assert.Equal(0, arenaInformation.Win);
            Assert.Equal(0, arenaInformation.Lose);
            Assert.Equal(ArenaInformation.MaxTicketCount, arenaInformation.Ticket);

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException($"{nameof(JoinArena)} : {row.ChampionshipId} / {round}");
            }

            Assert.Equal(0 * _currency, LegacyModule.GetBalance(state, _signer, _currency));
        }

        [Theory]
        [InlineData(9999)]
        public void Execute_SheetRowNotFoundException(int championshipId)
        {
            var avatarState = AvatarModule.GetAvatarState(_state, _avatarAddress);
            avatarState = GetAvatarState(avatarState, out var equipments, out var costumes);
            var state = AvatarModule.SetAvatarState(
                _state,
                _avatarAddress,
                avatarState,
                true,
                false,
                false,
                false);

            var action = new JoinArena()
            {
                championshipId = championshipId,
                round = 1,
                costumes = costumes,
                equipments = equipments,
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _signer,
                Random = new TestRandom(),
            }));
        }

        [Theory]
        [InlineData(123)]
        public void Execute_RoundNotFoundByIdsException(int round)
        {
            var avatarState = AvatarModule.GetAvatarState(_state, _avatarAddress);
            avatarState = GetAvatarState(avatarState, out var equipments, out var costumes);
            var state = AvatarModule.SetAvatarState(
                _state,
                _avatarAddress,
                avatarState,
                true,
                false,
                false,
                false);

            var action = new JoinArena()
            {
                championshipId = 1,
                round = round,
                costumes = costumes,
                equipments = equipments,
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<RoundNotFoundException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = 1,
            }));
        }

        [Theory]
        [InlineData(8)]
        public void Execute_NotEnoughMedalException(int round)
        {
            var avatarState = AvatarModule.GetAvatarState(_state, _avatarAddress);
            GetAvatarState(avatarState, out var equipments, out var costumes);
            var preCurrency = 99800100000 * _currency;
            var context = new ActionContext();
            var state = LegacyModule.MintAsset(_state, context, _signer, preCurrency);

            var action = new JoinArena()
            {
                championshipId = 1,
                round = round,
                costumes = costumes,
                equipments = equipments,
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<NotEnoughMedalException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = 100,
            }));
        }

        [Theory]
        [InlineData(6, 0)] // discounted_entrance_fee
        [InlineData(8, 100)] // entrance_fee
        public void Execute_NotEnoughFungibleAssetValueException(int round, long blockIndex)
        {
            var avatarState = AvatarModule.GetAvatarState(_state, _avatarAddress);
            GetAvatarState(avatarState, out var equipments, out var costumes);
            var state = AvatarModule.SetAvatarState(
                _state,
                _avatarAddress,
                avatarState,
                true,
                false,
                false,
                false);

            var action = new JoinArena()
            {
                championshipId = 1,
                round = round,
                costumes = costumes,
                equipments = equipments,
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<NotEnoughFungibleAssetValueException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = blockIndex,
            }));
        }

        [Fact]
        public void Execute_ArenaScoreAlreadyContainsException()
        {
            var avatarState = AvatarModule.GetAvatarState(_state, _avatarAddress);
            avatarState = GetAvatarState(avatarState, out var equipments, out var costumes);
            var state = AvatarModule.SetAvatarState(
                _state,
                _avatarAddress,
                avatarState,
                true,
                false,
                false,
                false);

            var action = new JoinArena()
            {
                championshipId = 1,
                round = 1,
                costumes = costumes,
                equipments = equipments,
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
            };

            state = action.Execute(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _signer,
                    Random = _random,
                    Rehearsal = false,
                    BlockIndex = 1,
                });

            Assert.Throws<ArenaScoreAlreadyContainsException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = 2,
            }));
        }

        [Fact]
        public void Execute_ArenaScoreAlreadyContainsException2()
        {
            const int championshipId = 1;
            const int round = 1;

            var avatarState = AvatarModule.GetAvatarState(_state, _avatarAddress);
            avatarState = GetAvatarState(avatarState, out var equipments, out var costumes);
            var state = AvatarModule.SetAvatarState(
                _state,
                _avatarAddress,
                avatarState,
                true,
                false,
                false,
                false);

            var arenaScoreAdr = ArenaScore.DeriveAddress(_avatarAddress, championshipId, round);
            var arenaScore = new ArenaScore(_avatarAddress, championshipId, round);
            state = LegacyModule.SetState(state, arenaScoreAdr, arenaScore.Serialize());

            var action = new JoinArena()
            {
                championshipId = championshipId,
                round = round,
                costumes = costumes,
                equipments = equipments,
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<ArenaScoreAlreadyContainsException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = 1,
            }));
        }

        [Fact]
        public void Execute_ArenaInformationAlreadyContainsException()
        {
            const int championshipId = 1;
            const int round = 1;

            var avatarState = AvatarModule.GetAvatarState(_state, _avatarAddress);
            avatarState = GetAvatarState(avatarState, out var equipments, out var costumes);
            var state = AvatarModule.SetAvatarState(
                _state,
                _avatarAddress,
                avatarState,
                true,
                false,
                false,
                false);

            var arenaInformationAdr = ArenaInformation.DeriveAddress(_avatarAddress, championshipId, round);
            var arenaInformation = new ArenaInformation(_avatarAddress, championshipId, round);
            state = LegacyModule.SetState(state, arenaInformationAdr, arenaInformation.Serialize());

            var action = new JoinArena()
            {
                championshipId = championshipId,
                round = round,
                costumes = costumes,
                equipments = equipments,
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<ArenaInformationAlreadyContainsException>(() => action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = 1,
            }));
        }

        [Fact]
        public void Execute_NotEnoughClearedStageLevelException()
        {
            var action = new JoinArena()
            {
                championshipId = 1,
                round = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatar2Address,
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() => action.Execute(new ActionContext()
            {
                PreviousState = _state,
                Signer = _signer2,
                Random = new TestRandom(),
            }));
        }

        [Theory]
        [InlineData(0, 30001, 1, 30001, typeof(DuplicatedRuneIdException))]
        [InlineData(1, 10002, 1, 30001, typeof(DuplicatedRuneSlotIndexException))]
        public void ExecuteDuplicatedException(int slotIndex, int runeId, int slotIndex2, int runeId2, Type exception)
        {
            int championshipId = 1;
            int round = 1;
            var arenaSheet = LegacyModule.GetSheet<ArenaSheet>(_state);
            if (!arenaSheet.TryGetValue(championshipId, out var row))
            {
                throw new SheetRowNotFoundException(
                    nameof(ArenaSheet), $"championship Id : {championshipId}");
            }

            var avatarState = AvatarModule.GetAvatarState(_state, _avatarAddress);
            avatarState = GetAvatarState(avatarState, out var equipments, out var costumes);
            avatarState = AddMedal(avatarState, row, 80);

            var context = new ActionContext();
            var ncgCurrency = LegacyModule.GetGoldCurrency(_state);
            var state = LegacyModule.MintAsset(_state, context, _signer, 99999 * ncgCurrency);

            var unlockRuneSlot = new UnlockRuneSlot()
            {
                AvatarAddress = _avatarAddress,
                SlotIndex = 1,
            };

            state = unlockRuneSlot.Execute(
                new ActionContext
                {
                    BlockIndex = 1,
                    PreviousState = state,
                    Signer = _signer,
                    Random = new TestRandom(),
                });

            var action = new JoinArena()
            {
                championshipId = championshipId,
                round = round,
                costumes = costumes,
                equipments = equipments,
                runeInfos = new List<RuneSlotInfo>()
                {
                    new RuneSlotInfo(slotIndex, runeId),
                    new RuneSlotInfo(slotIndex2, runeId2),
                },
                avatarAddress = _avatarAddress,
            };

            Assert.Throws(exception, () => action.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = _signer,
                Random = new TestRandom(),
            }));
        }
    }
}
