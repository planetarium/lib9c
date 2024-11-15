using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.State;
using Nekoyume.Module;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class RetrieveAvatarAssets: ActionBase
    {
        public const string TypeIdentifier = "retrieve_avatar_assets";
        public Address AvatarAddress;

        public RetrieveAvatarAssets()
        {
        }

        public RetrieveAvatarAssets(Address avatarAddress)
        {
            AvatarAddress = avatarAddress;
        }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Dictionary.Empty.Add("a", AvatarAddress.Serialize()));

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary)((Dictionary)plainValue)["values"];
            AvatarAddress = asDict["a"].ToAddress();
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            Address signer = context.Signer;
            var state = context.PreviousState;
            var agentState = state.GetAgentState(signer);
            if (agentState is not null && agentState.avatarAddresses.ContainsValue(AvatarAddress))
            {
                var currency = state.GetGoldCurrency();
                var balance = state.GetBalance(AvatarAddress, currency);
                return state.TransferAsset(context, AvatarAddress, signer, balance);
            }

            throw new FailedLoadStateException($"signer({signer}) does not contains avatar address({AvatarAddress}).");
        }
    }
}
