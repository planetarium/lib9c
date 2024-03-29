namespace Lib9c.Tests.Action.DPoS
{
    using Libplanet.Action.State;
    using Libplanet.Crypto;

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
    }
}
