using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [ActionType("reincarnation")]
    public class Reincarnation : ActionBase
    {
        public Reincarnation()
        {
        }

        public override IValue PlainValue => Null.Value;
        public override void LoadPlainValue(IValue plainValue)
        {
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var signer = context.Signer;
            var states = context.PreviousStates.Mead(Addresses.Heidrun, 1);
            var contractAddress = signer.Derive(nameof(BringEinheri));
            if (states.TryGetState(contractAddress, out List _))
            {
                throw new AlreadyReceivedException("");
            }

            return states
                .TransferAsset(Addresses.Heidrun, signer, 10 * Currencies.Mead)
                .SetState(
                    contractAddress,
                    List.Empty
                        .Add(Addresses.Heidrun.Serialize())
                        .Add(true.Serialize())
                );
        }
    }
}
