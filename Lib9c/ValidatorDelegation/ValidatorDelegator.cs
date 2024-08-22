#nullable enable
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Delegation;

namespace Nekoyume.ValidatorDelegation
{
    public class ValidatorDelegator : Delegator<ValidatorDelegatee, ValidatorDelegator>
    {
        public ValidatorDelegator(Address address, ValidatorRepository? repository = null)
            : base(address, repository)
        {
        }

        public ValidatorDelegator(Address address, IValue bencoded, ValidatorRepository? repository = null)
            : base(address, bencoded, repository)
        {
        }

        public ValidatorDelegator(Address address, List bencoded, ValidatorRepository? repository = null)
            : base(address, bencoded, repository)
        {
        }
    }
}
