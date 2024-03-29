using Libplanet.Crypto;

namespace Nekoyume.Action.DPoS.Exception
{
    public class DuplicatedValidatorException : System.Exception
    {
        public DuplicatedValidatorException(Address address)
            : base($"Validator {address} is duplicated")
        {
        }
    }
}
