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
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Market;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class BuyProductTest
    {
        private readonly Address _sellerAgentAddress;
        private readonly Address _sellerAvatarAddress;
        private readonly Address _sellerAgentAddress2;
        private readonly Address _sellerAvatarAddress2;
        private readonly Address _buyerAgentAddress;
        private readonly Address _buyerAvatarAddress;
        private readonly AvatarState _buyerAvatarState;
        private readonly TableSheets _tableSheets;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly Guid _orderId;
        private IAccountStateDelta _initialState;

        public BuyProductTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new State();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            _goldCurrencyState = new GoldCurrencyState(currency);

            _sellerAgentAddress = new PrivateKey().ToAddress();
            var sellerAgentState = new AgentState(_sellerAgentAddress);
            _sellerAvatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
            var sellerAvatarState = new AvatarState(
                _sellerAvatarAddress,
                _sellerAgentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            sellerAgentState.avatarAddresses[0] = _sellerAvatarAddress;

            _sellerAgentAddress2 = new PrivateKey().ToAddress();
            var agentState2 = new AgentState(_sellerAgentAddress2);
            _sellerAvatarAddress2 = new PrivateKey().ToAddress();
            var sellerAvatarState2 = new AvatarState(
                _sellerAvatarAddress2,
                _sellerAgentAddress2,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            agentState2.avatarAddresses[0] = _sellerAvatarAddress2;

            _buyerAgentAddress = new PrivateKey().ToAddress();
            var buyerAgentState = new AgentState(_buyerAgentAddress);
            _buyerAvatarAddress = new PrivateKey().ToAddress();
            _buyerAvatarState = new AvatarState(
                _buyerAvatarAddress,
                _buyerAgentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            buyerAgentState.avatarAddresses[0] = _buyerAvatarAddress;

            _orderId = new Guid("6d460c1a-755d-48e4-ad67-65d5f519dbc8");
            _initialState = _initialState
                .SetState(GoldCurrencyState.Address, _goldCurrencyState.Serialize())
                .SetState(_sellerAgentAddress, sellerAgentState.Serialize())
                .SetState(_sellerAvatarAddress, sellerAvatarState.Serialize())
                .SetState(_sellerAgentAddress2, agentState2.Serialize())
                .SetState(_sellerAvatarAddress2, sellerAvatarState2.Serialize())
                .SetState(_buyerAgentAddress, buyerAgentState.Serialize())
                .SetState(_buyerAvatarAddress, _buyerAvatarState.Serialize())
                .MintAsset(_buyerAgentAddress, _goldCurrencyState.Currency * 100);
        }

        [Fact]
        public void Execute_InvalidAddress()
        {
            var productId = Guid.NewGuid();
            var action = new BuyProduct
            {
                AvatarAddress = _buyerAvatarAddress,
                ProductInfos = new List<ProductInfo>
                {
                    new ProductInfo
                    {
                        AvatarAddress = _buyerAvatarAddress,
                        AgentAddress = _sellerAgentAddress,
                        Legacy = false,
                        Price = 1 * _goldCurrencyState.Currency,
                        ProductId = productId,
                        Type = ProductType.NonFungible,
                    },
                },
            };

            var actionContext = new ActionContext
            {
                Signer = _buyerAgentAddress,
                BlockIndex = 1L,
                PreviousStates = _initialState,
                Random = new TestRandom(),
            };
            Assert.Throws<InvalidAddressException>(() => action.Execute(actionContext));
        }

        [Fact]
        public void Execute_ProductNotFoundException()
        {
            var productId = Guid.NewGuid();
            var action = new BuyProduct
            {
                AvatarAddress = _buyerAvatarAddress,
                ProductInfos = new List<ProductInfo>
                {
                    new ProductInfo
                    {
                        AvatarAddress = _sellerAvatarAddress,
                        AgentAddress = _sellerAgentAddress,
                        Legacy = false,
                        Price = 1 * _goldCurrencyState.Currency,
                        ProductId = productId,
                        Type = ProductType.NonFungible,
                    },
                },
            };

            _initialState = _initialState.SetState(
                ProductsState.DeriveAddress(_sellerAvatarAddress), new ProductsState().Serialize());
            var actionContext = new ActionContext
            {
                Signer = _buyerAgentAddress,
                BlockIndex = 1L,
                PreviousStates = _initialState,
                Random = new TestRandom(),
            };
            Assert.Throws<ProductNotFoundException>(() => action.Execute(actionContext));
        }
    }
}
