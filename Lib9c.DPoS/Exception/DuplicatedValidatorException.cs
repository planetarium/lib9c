using Libplanet.Crypto;

namespace Lib9c.DPoS.Exception
{
    public class DuplicatedValidatorException : System.Exception
    {
        public DuplicatedValidatorException(Address address)
            : base($"Validator {address} is duplicated")
        {
        }
    }
}
