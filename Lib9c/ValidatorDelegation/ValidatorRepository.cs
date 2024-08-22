#nullable enable
using Libplanet.Action.State;
using Libplanet.Action;
using Nekoyume.Delegation;
using Bencodex.Types;
using Libplanet.Crypto;

namespace Nekoyume.ValidatorDelegation
{
    public class ValidatorRepository : DelegationRepository
    {
        private readonly Address validatorListAddress = Addresses.ValidatorList;

        private IAccount _validatorList;

        public ValidatorRepository(IWorld world, IActionContext context)
            : base(world, context)
        {
            _validatorList = world.GetAccount(validatorListAddress);
        }

        public ValidatorList GetValidatorList()
        {
            IValue? value = _validatorList.GetState(ValidatorList.Address);
            return value is IValue bencoded
                ? new ValidatorList(bencoded)
                : new ValidatorList();
        }

        public void SetValidatorList(ValidatorList validatorList)
        {
            _validatorList = _validatorList.SetState(ValidatorList.Address, validatorList.Bencoded);
        }
    }
}
