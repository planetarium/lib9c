using Libplanet.Crypto;

namespace Lib9c.DPoS.Exception
{
    public class NullValidatorException : System.Exception
    {
        public NullValidatorException(Address address)
            : base($"Validator {address} not found")
        {
        }
    }
}
