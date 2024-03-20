using Lib9c.Tests.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.DPoS.Tests
{
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
