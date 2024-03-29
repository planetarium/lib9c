using Libplanet.Types.Assets;

namespace Nekoyume.Action.DPoS.Exception
{
    public class InsufficientFungibleAssetValueException : System.Exception
    {
        public InsufficientFungibleAssetValueException(
            FungibleAssetValue required, FungibleAssetValue actual, string message)
            : base($"{message}, required : {required} > actual : {actual}")
        {
        }
    }
}
