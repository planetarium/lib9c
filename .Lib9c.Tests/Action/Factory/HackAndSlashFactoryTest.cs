namespace Lib9c.Tests.Action.Factory
{
    using System;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Action.Factory;
    using Xunit;

    public class HackAndSlashFactoryTest
    {
        [Theory]
        [InlineData(
            Nekoyume.BlockChain.Policy.BlockPolicySource.V100340ObsoleteIndex - 1,
            typeof(HackAndSlash18)
        )]
        [InlineData(
            Nekoyume.BlockChain.Policy.BlockPolicySource.V100340ObsoleteIndex,
            typeof(HackAndSlash18)
        )]
        [InlineData(
            Nekoyume.BlockChain.Policy.BlockPolicySource.V100340ObsoleteIndex + 1,
            typeof(HackAndSlash19)
        )]
        [InlineData(
            Nekoyume.BlockChain.Policy.BlockPolicySource.V100360ObsoleteIndex - 1,
            typeof(HackAndSlash19)
        )]
        [InlineData(
            Nekoyume.BlockChain.Policy.BlockPolicySource.V100360ObsoleteIndex,
            typeof(HackAndSlash19)
        )]
        [InlineData(
            Nekoyume.BlockChain.Policy.BlockPolicySource.V100360ObsoleteIndex + 1,
            typeof(HackAndSlash)
        )]
        public void CreateByBlockIndex_Success(long blockIndex, Type type)
        {
            var addr = new PrivateKey().ToAddress();
            var action = HackAndSlashFactory.CreateByBlockIndex(blockIndex, addr, 1, 1);
            Assert.Equal(type, action.GetType());
        }

        [Theory]
        [InlineData(18, typeof(HackAndSlash18))]
        [InlineData(19, typeof(HackAndSlash19))]
        [InlineData(20, typeof(HackAndSlash))]
        public void CreateByVersion_Success(int version, Type type)
        {
            var addr = new PrivateKey().ToAddress();
            var action = HackAndSlashFactory.CreateByVersion(version, addr, 1, 1);
            Assert.Equal(type, action.GetType());
        }

        [Theory]
        [InlineData(17)]
        [InlineData(21)]
        public void CreateByVersion_Failure(int version)
        {
            var addr = new PrivateKey().ToAddress();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                HackAndSlashFactory.CreateByVersion(version, addr, 1, 1));
        }
    }
}
