using Libplanet.Types.Assets;

namespace Nekoyume.Action.DPoS.Exception
{
    public class InvalidCurrencyException : System.Exception
    {
        public InvalidCurrencyException(Currency expected, Currency actual)
            : base($"Expected {expected}, found {actual}")
        {
        }
    }
}
