using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Extensions;
using Nekoyume.Model.Exceptions;
using Nekoyume.Model.State;

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
            var account = world.GetAccount(ReservedAddresses.LegacyAccount);
            var contractAddress = AgentAddress.GetPledgeAddress();
            if (account.TryGetState(contractAddress, out List contract))
            {
                if (signer != contract[0].ToAddress())
                {
                    throw new InvalidAddressException($"{signer} is not patron.");
                }

                var balance = account.GetBalance(AgentAddress, Currencies.Mead);
                if (balance > 0 * Currencies.Mead)
                {
                    account = account.TransferAsset(context, AgentAddress, signer, balance);
                }

                account = account.SetState(contractAddress, Null.Value);
                return world.SetAccount(account);
            }

            throw new FailedLoadStateException("failed to find pledge.");
        }
    }
}
