namespace Lib9c.Tests.Policy
{
    using System;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.Loader;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Blocks;
    using Libplanet.Types.Tx;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Loader;
    using Nekoyume.Blockchain.Policy;
    using Xunit;

    public class BlockPolicySourceTest
    {
        private static readonly BlockHash OdinGenesisHash = BlockHash.FromString(
            "4582250d0da33b06779a8475d283d5dd210c683b9b999d74d03fac4f58fa6bce"
        );

        private static readonly BlockHash HeimdallGenesisHash = BlockHash.FromString(
            "ade4c29773fe83c1a51da6a667a5a26f08848155674637d43fe636b94a320514"
        );

        [Fact]
        public void IsObsolete()
        {
            var buy = new Buy
            {
                buyerAvatarAddress = new PrivateKey().Address,
                purchaseInfos = Array.Empty<PurchaseInfo>(),
            };
            var odinTx = Transaction.Create(
                0,
                new PrivateKey(),
                OdinGenesisHash,
                new[] { buy.PlainValue, }
            );
            var heimdallTx = Transaction.Create(
                0,
                new PrivateKey(),
                HeimdallGenesisHash,
                new[] { buy.PlainValue, }
            );
            var ncActionLoader = new NCActionLoader();

            Assert.False(BlockPolicySource.IsObsolete(odinTx, ncActionLoader, 1));
            Assert.True(BlockPolicySource.IsObsolete(odinTx, ncActionLoader, 8_324_911));
            Assert.True(BlockPolicySource.IsObsolete(heimdallTx, ncActionLoader, 1));

            var odinTx2 = Transaction.Create(
                0,
                new PrivateKey(),
                OdinGenesisHash,
                new[] { Dictionary.Empty, }
            );
            var heimdallTx2 = Transaction.Create(
                0,
                new PrivateKey(),
                HeimdallGenesisHash,
                new[] { Dictionary.Empty, }
            );
            var newActionLoader = new SingleActionLoader(typeof(NewAction));

            Assert.False(BlockPolicySource.IsObsolete(odinTx2, newActionLoader, 1));
            Assert.False(BlockPolicySource.IsObsolete(odinTx2, newActionLoader, 50));
            Assert.True(BlockPolicySource.IsObsolete(odinTx2, newActionLoader, 103)); // Due to +2 offset for odin bug
            Assert.False(BlockPolicySource.IsObsolete(heimdallTx2, newActionLoader, 1));
            Assert.True(BlockPolicySource.IsObsolete(heimdallTx2, newActionLoader, 51));
        }

        [ActionObsolete(Planet.Odin, 100)]
        [ActionObsolete(Planet.Heimdall, 50)]
        private class NewAction : IAction
        {
            public IValue PlainValue => (Bencodex.Types.Integer)42;

            public IWorld Execute(IActionContext context)
            {
                throw new System.NotImplementedException();
            }

            public void LoadPlainValue(IValue plainValue)
            {
            }
        }
    }
}
