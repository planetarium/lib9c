using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Extensions;
using Nekoyume.Model.Exceptions;
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
            var world = context.PreviousState;
            var contractAddress = AgentAddress.GetPledgeAddress();
            if (LegacyModule.TryGetState(world, contractAddress, out List contract))
            {
                if (signer != contract[0].ToAddress())
                {
                    throw new InvalidAddressException($"{signer} is not patron.");
                }

                var balance = LegacyModule.GetBalance(world, AgentAddress, Currencies.Mead);
                if (balance > 0 * Currencies.Mead)
                {
                    world = LegacyModule.TransferAsset(
                        world,
                        context,
                        AgentAddress,
                        signer,
                        balance);
                }

                return LegacyModule.SetState(world, contractAddress, Null.Value);
            }

            throw new FailedLoadStateException("failed to find pledge.");
        }
    }
}
