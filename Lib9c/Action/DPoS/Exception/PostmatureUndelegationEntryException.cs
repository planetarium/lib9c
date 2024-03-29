using Libplanet.Crypto;

namespace Nekoyume.Action.DPoS.Exception
{
    public class PostmatureUndelegationEntryException : System.Exception
    {
        public PostmatureUndelegationEntryException(
            long blockHeight, long completionBlockHeight, Address address)
            : base($"UndelegationEntry {address} is postmatured, " +
                  $"blockHeight : {blockHeight} > completionBlockHeight : {completionBlockHeight}")
        {
        }
    }
}
