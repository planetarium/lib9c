using Libplanet.Crypto;

namespace Nekoyume.Action.DPoS.Exception
{
    public class JailedValidatorException : System.Exception
    {
        public JailedValidatorException(Address address)
            : base($"Validator {address} is jailed")
        {
        }
    }
}
