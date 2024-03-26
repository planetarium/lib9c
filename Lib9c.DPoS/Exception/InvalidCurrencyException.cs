using Libplanet.Types.Assets;

namespace Lib9c.DPoS.Exception
{
    public class InvalidCurrencyException : System.Exception
    {
        public InvalidCurrencyException(Currency expected, Currency actual)
            : base($"Expected {expected}, found {actual}")
        {
        }
    }
}
