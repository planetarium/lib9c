namespace Lib9c.Tests.Action.DPoS
{
    using System.Numerics;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action.DPoS.Misc;

    public class PoSTest
    {
        protected static IWorld InitializeStates()
        {
            return new World(new MockWorldState());
        }

        protected static Address CreateAddress()
        {
            PrivateKey privateKey = new PrivateKey();
            return privateKey.Address;
        }

        protected static FungibleAssetValue ShareFromGovernance(FungibleAssetValue governanceToken)
            => FungibleAssetValue.FromRawValue(Asset.Share, governanceToken.RawValue);

        protected static FungibleAssetValue ShareFromGovernance(BigInteger amount)
            => ShareFromGovernance(Asset.GovernanceToken * amount);
    }
}
