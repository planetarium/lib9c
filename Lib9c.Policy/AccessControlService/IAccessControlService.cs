using System.Threading.Tasks;
using Libplanet.Crypto;

namespace Nekoyume.Blockchain
{
    public interface IAccessControlService
    {
        public Task<int?> GetTxQuotaAsync(Address address);
    }
}
