using System.Security.Cryptography;
using Libplanet;

namespace Lib9c.Model.Item
{
    public interface IFungibleItem: IItem
    {
        HashDigest<SHA256> FungibleId { get; }
    }
}
