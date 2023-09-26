using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Extensions;
using Nekoyume.Model.Exceptions;
using Nekoyume.Model.State;
using Nekoyume.Module;

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
            var contractAddress = signer.GetPledgeAddress();
            if (!LegacyModule.TryGetState(world, contractAddress, out List contract))
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

            return LegacyModule.SetState(
                world,
                contractAddress,
                List.Empty
                    .Add(PatronAddress.Serialize())
                    .Add(true.Serialize())
                    .Add(contract[2])
            );
        }
    }
}
