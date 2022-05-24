using System;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("faucet_asset")]
    [ActionObsolete(FaucetExpiredIndex)]
    public class FaucetAsset : ActionBase
    {
        public const long FaucetExpiredIndex = 1_500_000L;
        public Address Sender;
        public Address Recipient;
        public FungibleAssetValue Amount;

        public FaucetAsset()
        {
        }
        public FaucetAsset(Address sender, Address recipient, FungibleAssetValue amount)
        {
            Sender = sender;
            Recipient = recipient;
            Amount = amount;
        }

        public override IValue PlainValue => List.Empty
            .Add(Sender.Serialize())
            .Add(Recipient.Serialize())
            .Add(Amount.Serialize());

        public override void LoadPlainValue(IValue plainValue)
        {
            var asList = (List) plainValue;
            Sender = asList[0].ToAddress();
            Recipient = asList[1].ToAddress();
            Amount = asList[2].ToFungibleAssetValue();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states.MarkBalanceChanged(Amount.Currency, Sender, Recipient);
            }

            CheckObsolete(FaucetExpiredIndex, context);
            CheckPermission(context);
            CheckAsset(context, Sender, Recipient, Amount, true);

            if (states.TryGetState(Addresses.Admin, out Dictionary adminDict))
            {
                var adminState = new AdminState(adminDict);
                return states.TransferAsset(adminState.AdminAddress, Recipient, Amount);
            }

            return states;
        }
    }
}
