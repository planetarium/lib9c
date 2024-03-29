using Libplanet.Crypto;

namespace Nekoyume.Action.DPoS.Exception
{
    public class InvalidExchangeRateException : System.Exception
    {
        public InvalidExchangeRateException(Address address)
            : base($"Exchange of Validator {address} is invalid")
        {
        }
    }
}
