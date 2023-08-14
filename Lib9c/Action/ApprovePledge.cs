using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Extensions;
using Nekoyume.Model.Exceptions;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [ActionType(TypeIdentifier)]
    public class ApprovePledge : ActionBase
    {
        public const string TypeIdentifier = "approve_pledge";
        public ApprovePledge()
        {
        }

        public Address PatronAddress;
        public override IValue PlainValue =>
        Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", PatronAddress.Serialize());
        public override void LoadPlainValue(IValue plainValue)
        {
            PatronAddress = ((Dictionary)plainValue)["values"].ToAddress();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            Address signer = context.Signer;
            var world = context.PreviousState;
            var account = world.GetAccount(ReservedAddresses.LegacyAccount);
            var contractAddress = signer.GetPledgeAddress();
            if (!account.TryGetState(contractAddress, out List contract))
            {
                throw new FailedLoadStateException("failed to find requested pledge.");
            }

            if (contract[0].ToAddress() != PatronAddress)
            {
                throw new InvalidAddressException("invalid patron address.");
            }

            if (contract[1].ToBoolean())
            {
                throw new AlreadyContractedException($"{signer} already contracted.");
            }

            account = account.SetState(
                contractAddress,
                List.Empty
                    .Add(PatronAddress.Serialize())
                    .Add(true.Serialize())
                    .Add(contract[2])
            );
            return world.SetAccount(account);
        }
    }
}
