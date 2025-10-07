namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Action;
    using Lib9c.Arena;
    using Lib9c.Model.Arena;
    using Lib9c.Model.EnumType;
    using Lib9c.Model.Item;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Lib9c.TableData;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class JoinArenaTest
    {
        private readonly IWorld _world;
        private readonly TableSheets _tableSheets;
        private readonly AgentState _myAgentState;
        private readonly AvatarState _myAvatarState;

        public JoinArenaTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
            (_world, _tableSheets) = InitializeUtil.Initialize();
            (_world, _myAgentState) = InitializeUtil.AddAgent(_world);
            (_world, _myAvatarState, _) = InitializeUtil.AddAvatar(
                _world,
                _tableSheets.GetAvatarSheets(),
                _myAgentState.address);
        }

        [Fact]
        public void Execute_OffSeason_Success()
        {
            Execute(
                _world,
                _myAgentState.address,
                0,
                0,
                _myAvatarState.address,
                1,
                1,
                new List<Guid>(),
                new List<Guid>(),
                new List<RuneSlotInfo>());
        }

        [Fact]
        public void Execute_Season_Success()
        {
            var row = _tableSheets.ArenaSheet.OrderedList!.First(
                e =>
                    e.Round.Any(
                        e2 =>
                            e2.ArenaType == ArenaType.Season &&
                            e2.EntranceFee > 0));
            var roundData = row.Round.First(
                e =>
                    e.ArenaType == ArenaType.Season &&
                    e.EntranceFee > 0);
            var blockIndex = roundData.StartBlockIndex;
            var fee = ArenaHelper.GetEntranceFee(
                roundData,
                blockIndex,
                _myAvatarState.level);
            var world = _world.MintAsset(new ActionContext(), _myAgentState.address, fee);
            Execute(
                world,
                _myAgentState.address,
                blockIndex,
                0,
                _myAvatarState.address,
                row.ChampionshipId,
                roundData.Round,
                new List<Guid>(),
                new List<Guid>(),
                new List<RuneSlotInfo>());
        }

        [Fact]
        public void Execute_Championship_Success()
        {
            var row = _tableSheets.ArenaSheet.OrderedList!.First(
                e =>
                    e.Round.Any(
                        e2 =>
                            e2.ArenaType == ArenaType.Championship &&
                            e2.RequiredMedalCount > 0));
            Assert.True(row.TryGetChampionshipRound(out var roundData));
            var blockIndex = roundData.StartBlockIndex;
            var fee = ArenaHelper.GetEntranceFee(
                roundData,
                blockIndex,
                _myAvatarState.level);
            var world = _world.MintAsset(new ActionContext(), _myAgentState.address, fee);
            var seasonRound = row.Round.First(e => e.ArenaType == ArenaType.Season);
            var itemSheetId = seasonRound.MedalId;
            Assert.True(_tableSheets.ItemSheet.TryGetValue(itemSheetId, out var itemRow));
            var item = ItemFactory.CreateItem(itemRow, new TestRandom());
            var inventory = _world.GetInventoryV2(_myAvatarState.address);
            inventory.AddItem(item, roundData.RequiredMedalCount);
            world = world.SetInventory(_myAvatarState.address, inventory);

            Execute(
                world,
                _myAgentState.address,
                blockIndex,
                0,
                _myAvatarState.address,
                row.ChampionshipId,
                roundData.Round,
                new List<Guid>(),
                new List<Guid>(),
                new List<RuneSlotInfo>());
        }

        [Theory]
        [InlineData(int.MaxValue)]
        public void Execute_SheetRowNotFoundException(int championshipId)
        {
            Assert.Throws<SheetRowNotFoundException>(
                () => Execute(
                    _world,
                    _myAgentState.address,
                    0,
                    0,
                    _myAvatarState.address,
                    championshipId,
                    1,
                    new List<Guid>(),
                    new List<Guid>(),
                    new List<RuneSlotInfo>()));
        }

        [Theory]
        [InlineData(10)]
        public void Execute_RoundNotFoundByIdsException(int round)
        {
            Assert.Throws<RoundNotFoundException>(
                () => Execute(
                    _world,
                    _myAgentState.address,
                    0,
                    0,
                    _myAvatarState.address,
                    1,
                    round,
                    new List<Guid>(),
                    new List<Guid>(),
                    new List<RuneSlotInfo>()));
        }

        [Fact]
        public void Execute_NotEnoughFungibleAssetValueException()
        {
            var row = _tableSheets.ArenaSheet.OrderedList!.First(
                e =>
                    e.Round.Any(
                        e2 =>
                            e2.ArenaType == ArenaType.Season &&
                            e2.EntranceFee > 0));
            var roundData = row.Round.First(
                e =>
                    e.ArenaType == ArenaType.Season &&
                    e.EntranceFee > 0);
            var blockIndex = roundData.StartBlockIndex;

            // with 0 assets.
            Assert.Throws<NotEnoughFungibleAssetValueException>(
                () => Execute(
                    _world,
                    _myAgentState.address,
                    blockIndex,
                    0,
                    _myAvatarState.address,
                    row.ChampionshipId,
                    roundData.Round,
                    new List<Guid>(),
                    new List<Guid>(),
                    new List<RuneSlotInfo>()));

            // get discounted fee.
            var fee = ArenaHelper.GetEntranceFee(
                roundData,
                blockIndex - 1,
                _myAvatarState.level);
            var world = _world
                .MintAsset(new ActionContext(), _myAgentState.address, fee)
                .BurnAsset(new ActionContext(), _myAgentState.address, new FungibleAssetValue(fee.Currency, 0, 1));
            // with not enough assets in discount period.
            Assert.Throws<NotEnoughFungibleAssetValueException>(
                () => Execute(
                    world,
                    _myAgentState.address,
                    blockIndex - 1,
                    0,
                    _myAvatarState.address,
                    row.ChampionshipId,
                    roundData.Round,
                    new List<Guid>(),
                    new List<Guid>(),
                    new List<RuneSlotInfo>()));

            // get original fee.
            fee = ArenaHelper.GetEntranceFee(
                roundData,
                blockIndex,
                _myAvatarState.level);
            world = _world
                .MintAsset(new ActionContext(), _myAgentState.address, fee)
                .BurnAsset(new ActionContext(), _myAgentState.address, new FungibleAssetValue(fee.Currency, 0, 1));
            // with not enough assets in discount period.
            Assert.Throws<NotEnoughFungibleAssetValueException>(
                () => Execute(
                    world,
                    _myAgentState.address,
                    blockIndex,
                    0,
                    _myAvatarState.address,
                    row.ChampionshipId,
                    roundData.Round,
                    new List<Guid>(),
                    new List<Guid>(),
                    new List<RuneSlotInfo>()));
        }

        [Fact]
        public void Execute_NotEnoughMedalException()
        {
            var row = _tableSheets.ArenaSheet.OrderedList!.First(
                e =>
                    e.Round.Any(
                        e2 =>
                            e2.ArenaType == ArenaType.Championship &&
                            e2.RequiredMedalCount > 0));
            Assert.True(row.TryGetChampionshipRound(out var roundData));
            var blockIndex = roundData.StartBlockIndex;
            var fee = ArenaHelper.GetEntranceFee(
                roundData,
                blockIndex,
                _myAvatarState.level);
            var world = _world.MintAsset(new ActionContext(), _myAgentState.address, fee);
            Assert.Throws<NotEnoughMedalException>(
                () => Execute(
                    world,
                    _myAgentState.address,
                    blockIndex,
                    0,
                    _myAvatarState.address,
                    row.ChampionshipId,
                    roundData.Round,
                    new List<Guid>(),
                    new List<Guid>(),
                    new List<RuneSlotInfo>()));
        }

        private IWorld Execute(
            IWorld world,
            Address signer,
            long blockIndex,
            int randomSeed,
            Address avatarAddr,
            int championshipId,
            int round,
            List<Guid> costumes,
            List<Guid> equipments,
            List<RuneSlotInfo> runeInfos)
        {
            var action = new JoinArena
            {
                avatarAddress = avatarAddr,
                championshipId = championshipId,
                round = round,
                costumes = costumes,
                equipments = equipments,
                runeInfos = runeInfos,
            };
            world = action.Execute(
                new ActionContext
                {
                    PreviousState = world,
                    Signer = signer,
                    BlockIndex = blockIndex,
                    RandomSeed = randomSeed,
                });

            // check ItemSlotState
            var itemSlotState = new ItemSlotState(BattleType.Arena);
            itemSlotState.UpdateCostumes(costumes);
            itemSlotState.UpdateEquipment(equipments);
            var itemSlotStateAddr = ItemSlotState.DeriveAddress(avatarAddr, BattleType.Arena);
            Assert.Equal(itemSlotState.Serialize(), world.GetLegacyState(itemSlotStateAddr));

            // check RuneSlotState
            var runeSlotState = new RuneSlotState(BattleType.Arena);
            runeSlotState.UpdateSlot(runeInfos, _tableSheets.RuneListSheet);
            var runeSlotStateAddr = RuneSlotState.DeriveAddress(avatarAddr, BattleType.Arena);
            Assert.Equal(runeSlotState.Serialize(), world.GetLegacyState(runeSlotStateAddr));

            // check ArenaParticipant
            var avatarState = world.GetAvatarState(avatarAddr);
            var arenaParticipant = world.GetArenaParticipant(championshipId, round, avatarAddr);
            Assert.NotNull(arenaParticipant);
            Assert.Equal(avatarAddr, arenaParticipant.AvatarAddr);
            Assert.Equal(avatarState.name, arenaParticipant.Name);
            Assert.Equal(avatarState.GetPortraitId(), arenaParticipant.PortraitId);
            Assert.Equal(avatarState.level, arenaParticipant.Level);
            // Assert.Equal(cp, arenaParticipant.Cp);
            Assert.Equal(ArenaParticipant.DefaultScore, arenaParticipant.Score);
            Assert.Equal(ArenaParticipant.MaxTicketCount, arenaParticipant.Ticket);
            Assert.Equal(0, arenaParticipant.TicketResetCount);
            Assert.Equal(0, arenaParticipant.PurchasedTicketCount);
            Assert.Equal(0, arenaParticipant.Win);
            Assert.Equal(0, arenaParticipant.Lose);
            Assert.Equal(0, arenaParticipant.LastBattleBlockIndex);

            // check ArenaParticipants
            var arenaParticipantsAdr = ArenaParticipants.DeriveAddress(championshipId, round);
            var arenaParticipants = world.GetArenaParticipants(arenaParticipantsAdr, championshipId, round);
            Assert.Equal(arenaParticipantsAdr, arenaParticipants.Address);
            Assert.Contains(avatarAddr, arenaParticipants.AvatarAddresses);

            // check ArenaScore
            var arenaScoreAdr = ArenaScore.DeriveAddress(avatarAddr, championshipId, round);
            Assert.True(world.TryGetArenaScore(arenaScoreAdr, out var arenaScore));
            Assert.Equal(ArenaScore.ArenaScoreDefault, arenaScore.Score);

            // check ArenaInformation
            var arenaInformationAdr = ArenaInformation.DeriveAddress(avatarAddr, championshipId, round);
            Assert.True(world.TryGetArenaInformation(arenaInformationAdr, out var arenaInformation));
            Assert.Equal(arenaInformationAdr, arenaInformation.Address);
            Assert.Equal(0, arenaInformation.Win);
            Assert.Equal(0, arenaInformation.Lose);
            Assert.Equal(ArenaInformation.MaxTicketCount, arenaInformation.Ticket);
            Assert.Equal(0, arenaInformation.TicketResetCount);
            Assert.Equal(0, arenaInformation.PurchasedTicketCount);

            // check ArenaAvatarState
            var arenaAvatarStateAddr = ArenaAvatarState.DeriveAddress(avatarAddr);
            var arenaAvatarState = new ArenaAvatarState(avatarState);
            arenaAvatarState.UpdateCostumes(costumes);
            arenaAvatarState.UpdateEquipment(equipments);
            Assert.True(world.TryGetLegacyState(arenaAvatarStateAddr, out IValue arenaAvatarStateInWorld));
            Assert.Equal(arenaAvatarState.Serialize(), arenaAvatarStateInWorld);

            return world;
        }
    }
}
