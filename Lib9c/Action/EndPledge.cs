using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.State;
using Nekoyume.Module;

namespace Nekoyume.Action
{
    [ActionType("end_pledge")]
    public class EndPledge : ActionBase
    {
        public const string TypeIdentifier = "end_pledge";
        public EndPledge()
        {
        }

        public Address AgentAddress;
        public override IValue PlainValue =>
            Dictionary.Empty
                .Add("type_id", TypeIdentifier)
                .Add("values", AgentAddress.Serialize());
        public override void LoadPlainValue(IValue plainValue)
        {
            AgentAddress = ((Dictionary)plainValue)["values"].ToAddress();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            Address signer = context.Signer;
            var states = context.PreviousState;
            var contractAddress = AgentAddress.GetPledgeAddress();
            if (states.TryGetLegacyState(contractAddress, out List contract))
            {
                if (signer != contract[0].ToAddress())
                {
                    throw new InvalidAddressException($"{signer} is not patron.");
                }

                var balance = states.GetBalance(AgentAddress, Currencies.Mead);
                if (balance > 0 * Currencies.Mead)
                {
                    states = states.TransferAsset(context, AgentAddress, signer, balance);
                }
                return states.RemoveLegacyState(contractAddress);
            }

            throw new FailedLoadStateException("failed to find pledge.");
        }
    }
}
