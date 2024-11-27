namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static SerializeKeys;

    public class SellAndCancellationAndSellTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly IWorld _initialState;

        public SellAndCancellationAndSellTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var gold = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);

            _agentAddress = new PrivateKey().Address;
            _avatarAddress = _agentAddress.Derive("avatar");
            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses[0] = _avatarAddress;
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.GetAvatarSheets(),
                default
            );
            avatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                GameConfig.RequireClearedStageLevel.ActionsInShop);

            _initialState = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(GoldCurrencyState.Address, gold.Serialize())
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize())
                .SetAgentState(_agentAddress, agentState)
                .SetAvatarState(_avatarAddress, avatarState);

            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Fact]
        public void Execute_With_TradableMaterial()
        {
            var previousStates = _initialState;
            var apStoneRow = _tableSheets.MaterialItemSheet.OrderedList!.First(
                row =>
                    row.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(apStoneRow);
            var avatarState = previousStates.GetAvatarState(_avatarAddress);
            // Add 10 ap stones to inventory.
            avatarState.inventory.AddFungibleItem(apStone, 10);
            previousStates = previousStates.SetAvatarState(_avatarAddress, avatarState);

            // sell ap stones with count 1, 2, 3, 4.
            var sellBlockIndex = 1L;
            var random = new TestRandom();
            var orderIds = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
            var sellActions = new[]
            {
                GetSell(apStone, 1, orderIds[0]),
                GetSell(apStone, 2, orderIds[1]),
                GetSell(apStone, 3, orderIds[2]),
                GetSell(apStone, 4, orderIds[3]),
            };
            var nextStates = previousStates;
            foreach (var sellAction in sellActions)
            {
                nextStates = sellAction.Execute(
                    new ActionContext
                    {
                        Signer = _agentAddress,
                        PreviousState = nextStates,
                        BlockIndex = sellBlockIndex,
                        RandomSeed = random.Seed,
                    });
                // TODO: Check state.. inventory, orders..
            }

            // Check inventory does not have ap stones.
            var nextAvatarState = nextStates.GetAvatarState(_avatarAddress);
            Assert.False(
                nextAvatarState.inventory.RemoveFungibleItem(
                    apStone.FungibleId,
                    sellBlockIndex,
                    1));

            // Cancel sell orders.
            var sellCancellationActions = new[]
            {
                GetSellCancellation(orderIds[0], apStone),
                GetSellCancellation(orderIds[1], apStone),
                GetSellCancellation(orderIds[2], apStone),
                GetSellCancellation(orderIds[3], apStone),
            };
            foreach (var sellCancellationAction in sellCancellationActions)
            {
                nextStates = sellCancellationAction.Execute(
                    new ActionContext
                    {
                        Signer = _agentAddress,
                        PreviousState = nextStates,
                        BlockIndex = sellBlockIndex + 1L,
                        RandomSeed = random.Seed,
                    });
                // TODO: Check state.. inventory, orders..
            }

            // Check inventory has 10 ap stones.
            nextAvatarState = nextStates.GetAvatarState(_avatarAddress);
            Assert.True(
                nextAvatarState.inventory.RemoveFungibleItem(
                    apStone.FungibleId,
                    sellBlockIndex + 1L,
                    10));

            // Sell 10 ap stones at once.
            var newSellOrderId = Guid.NewGuid();
            var newSellAction = GetSell(apStone, 10, newSellOrderId);
            nextStates = newSellAction.Execute(
                new ActionContext
                {
                    Signer = _agentAddress,
                    PreviousState = nextStates,
                    BlockIndex = sellBlockIndex + 2L,
                    RandomSeed = random.Seed,
                });

            // Check inventory does not have ap stones.
            nextAvatarState = nextStates.GetAvatarState(_avatarAddress);
            Assert.False(
                nextAvatarState.inventory.RemoveFungibleItem(
                    apStone.FungibleId,
                    sellBlockIndex + 2L,
                    1));
        }

        private Sell GetSell(ITradableItem tradableItem, int count, Guid orderId) =>
            new ()
            {
                sellerAvatarAddress = _avatarAddress,
                tradableId = tradableItem.TradableId,
                count = count,
                price = new FungibleAssetValue(
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                    Currency.Legacy("NCG", 2, null),
#pragma warning restore CS0618
                    1,
                    0),
                itemSubType = tradableItem.ItemSubType,
                orderId = orderId,
            };

        private SellCancellation GetSellCancellation(Guid orderId, ITradableItem tradableItem)
        {
            return new SellCancellation()
            {
                orderId = orderId,
                tradableId = tradableItem.TradableId,
                sellerAvatarAddress = _avatarAddress,
                itemSubType = tradableItem.ItemSubType,
            };
        }
    }
}
