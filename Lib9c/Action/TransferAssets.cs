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
    [ActionType("transfer_assets")]
    public class TransferAssets : ActionBase
    {
        public TransferAssets()
        {
        }

        public TransferAssets(Address sender, Address recipientAgent, List<(Address, FungibleAssetValue)> amountInfo, string memo = null)
        {
            Sender = sender;
            RecipientAgent = recipientAgent;
            AmountInfo = amountInfo;

            TransferAsset.CheckMemoLength(memo);
            Memo = memo;
        }

        public Address Sender { get; private set; }
        public Address RecipientAgent { get; private set; }
        public List<(Address recipient, FungibleAssetValue amount)> AmountInfo { get; private set; }
        public string Memo { get; private set; }

        public override IValue PlainValue
        {
            get
            {
                var list = List.Empty;
                foreach (var (address, amount) in AmountInfo)
                {
                    list = list.Add(List.Empty.Add(address.Serialize()).Add(amount.Serialize()));
                }

                var dict = Dictionary.Empty
                    .Add("s", Sender.Serialize())
                    .Add("ra", RecipientAgent.Serialize())
                    .Add("a", list.Serialize());
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
            RecipientAgent = serialized["ra"].ToAddress();
            AmountInfo = new List<(Address, FungibleAssetValue)>();
            var list = (List)serialized["a"];
            foreach (var iValue in list)
            {
                var innerList = (List) iValue;
                AmountInfo.Add((innerList[0].ToAddress(), innerList[1].ToFungibleAssetValue()));
            }

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
                return AmountInfo.Aggregate(states, (current, info) => current.MarkBalanceChanged(info.amount.Currency, Sender, info.recipient));
            }

            if (Sender != context.Signer)
            {
                throw new InvalidTransferSignerException(context.Signer, Sender, RecipientAgent);
            }

            if (Sender == RecipientAgent)
            {
                throw new InvalidTransferRecipientException(Sender, RecipientAgent);
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

            AgentState recipientAgent = states.GetAgentState(RecipientAgent);
            foreach (var (recipient, amount) in AmountInfo)
            {
                if (!recipient.Equals(RecipientAgent) && !recipientAgent.avatarAddresses.Values.Contains(recipient))
                {
                    throw new AgentStateNotContainsAvatarAddressException("");
                }

                Currency currency = amount.Currency;
                if (!(currency.Minters is null) && (currency.Minters.Contains(Sender) || currency.Minters.Contains(RecipientAgent) || currency.Minters.Contains(recipient)))
                {
                    throw new InvalidTransferMinterException(
                        currency.Minters,
                        Sender,
                        RecipientAgent
                    );
                }

                states =  states.TransferAsset(Sender, recipient, amount);
            }

            return states;
        }
    }
}
