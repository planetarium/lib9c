using Libplanet.Crypto;

namespace Lib9c.DPoS.Exception
{
    public class JailedValidatorException : System.Exception
    {
        public JailedValidatorException(Address address)
            : base($"Validator {address} is jailed")
        {
        }
    }
}
