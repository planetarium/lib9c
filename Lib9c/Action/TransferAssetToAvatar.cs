using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("transfer_asset_to_avatar")]
    public class TransferAssetToAvatar : ActionBase
    {
        public TransferAssetToAvatar()
        {
        }

        public TransferAssetToAvatar(Address sender, Address recipient, Address recipientAgent, List<FungibleAssetValue> amounts, string memo = null)
        {
            Sender = sender;
            Recipient = recipient;
            RecipientAgent = recipientAgent;
            Amounts = amounts;

            TransferAsset.CheckMemoLength(memo);
            Memo = memo;
        }

        public Address Sender { get; private set; }
        public Address Recipient { get; private set; }
        public Address RecipientAgent { get; private set; }
        public List<FungibleAssetValue> Amounts { get; private set; }
        public string Memo { get; private set; }

        public override IValue PlainValue
        {
            get
            {
                var dict = Dictionary.Empty
                    .Add("s", Sender.Serialize())
                    .Add("r", Recipient.Serialize())
                    .Add("ra", RecipientAgent.Serialize())
                    .Add("a", Amounts.Select(a => a.Serialize()));
                if (!(Memo is null))
                {
                    dict = dict.Add("m", Memo.Serialize());
                }
                return dict;
            }
        }
        public override void LoadPlainValue(IValue plainValue)
        {
            var serialized = (Dictionary) plainValue;
            Sender = serialized["s"].ToAddress();
            Recipient = serialized["r"].ToAddress();
            RecipientAgent = serialized["ra"].ToAddress();
            Amounts = serialized["a"].ToList(StateExtensions.ToFungibleAssetValue);
            if (serialized.ContainsKey("m"))
            {
                Memo = serialized["m"].ToDotnetString();
            }
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return Amounts.Aggregate(states, (current, amount) => current.MarkBalanceChanged(amount.Currency, Sender, Recipient));
            }

            if (Sender != context.Signer)
            {
                var senderAgentState = states.GetAgentState(context.Signer);
                if (senderAgentState is null || !senderAgentState.avatarAddresses.Values.Contains(Sender))
                {
                    throw new InvalidTransferSignerException(context.Signer, Sender, Recipient);
                }
            }

            foreach (var address in new[]{ RecipientAgent, Recipient })
            {
                if (Sender == address)
                {
                    throw new InvalidTransferRecipientException(Sender, address);
                }
            }

            var recipientAgentState = states.GetAgentState(RecipientAgent);
            if (!recipientAgentState.avatarAddresses.Values.Contains(Recipient))
            {
                throw new AgentStateNotContainsAvatarAddressException("");
            }

            Address recipientAddress = RecipientAgent.Derive(ActivationKey.DeriveKey);

            // Check activation.
            if (states.GetState(recipientAddress) is null && states.GetState(Addresses.ActivatedAccount) is Dictionary asDict)
            {
                var activatedAccountsState = new ActivatedAccountsState(asDict);
                var activatedAccounts = activatedAccountsState.Accounts;
                // if ActivatedAccountsState is empty, all user is activate.
                if (activatedAccounts.Count != 0
                    && !activatedAccounts.Contains(RecipientAgent))
                {
                    throw new InvalidTransferUnactivatedRecipientException(Sender, RecipientAgent);
                }
            }

            foreach (var amount in Amounts)
            {
                Currency currency = amount.Currency;
                // Exclude NCG.
                if (!(currency.Minters is null))
                {
                    throw new InvalidTransferMinterException(
                        currency.Minters,
                        Sender,
                        Recipient
                    );
                }

                states =  states.TransferAsset(Sender, Recipient, amount);
            }

            return states;
        }
    }
}
