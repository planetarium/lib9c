using Bencodex;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/636
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionType("transfer_equipment")]
    public class TransferItem : ActionBase, ISerializable
    {
        private const int MemoMaxLength = 80;
        

        public TransferItem()
        {
        }

        public TransferItem(Address sender, Address recipient, Guid itemId, string memo = null)
        {
            Sender = sender;
            RecipientAvatarAddress = recipient;
            ItemId = itemId;

            CheckMemoLength(memo);
            Memo = memo;
        }

        protected TransferItem(SerializationInfo info, StreamingContext context)
        {
            var rawBytes = (byte[])info.GetValue("serialized", typeof(byte[]));
            Dictionary pv = (Dictionary) new Codec().Decode(rawBytes);

            LoadPlainValue(pv);
        }

        public Address Sender { get; private set; }
        public Address RecipientAvatarAddress { get; private set; }
        public Guid ItemId;
        public string Memo { get; private set; }

        public override IValue PlainValue
        {
            get
            {
                IEnumerable<KeyValuePair<IKey, IValue>> pairs = new[]
                {
                    new KeyValuePair<IKey, IValue>((Text) "sender", Sender.Serialize()),
                    new KeyValuePair<IKey, IValue>((Text) "recipient", RecipientAvatarAddress.Serialize()),
                    new KeyValuePair<IKey, IValue>((Text) "itemId", ItemId.Serialize()),
                };

                if (!(Memo is null))
                {
                    pairs = pairs.Append(new KeyValuePair<IKey, IValue>((Text) "memo", Memo.Serialize()));
                }

                return new Dictionary(pairs);
            }
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var state = context.PreviousStates;
            if (context.Rehearsal)
            {
                return state;
            }

            if (Sender != context.Signer)
            {
                throw new InvalidTransferSignerException(context.Signer, Sender, RecipientAvatarAddress);
            }

            Address recipientAddress = RecipientAvatarAddress.Derive(ActivationKey.DeriveKey);

            // Check new type of activation first.
            if (state.GetState(recipientAddress) is null && state.GetState(Addresses.ActivatedAccount) is Dictionary asDict )
            {
                var activatedAccountsState = new ActivatedAccountsState(asDict);
                var activatedAccounts = activatedAccountsState.Accounts;
                // if ActivatedAccountsState is empty, all user is activate.
                if (activatedAccounts.Count != 0
                    && !activatedAccounts.Contains(RecipientAvatarAddress))
                {
                    throw new InvalidTransferUnactivatedRecipientException(Sender, RecipientAvatarAddress);
                }
            }

            //Currency currency = Amount.Currency;
            //if (!(currency.Minters is null) &&
            //    (currency.Minters.Contains(Sender) || currency.Minters.Contains(RecipientAvatarAddress)))
            //{
            //    throw new InvalidTransferMinterException(
            //        currency.Minters,
            //        Sender,
            //        RecipientAvatarAddress
            //   );
            //}

            return state;//.TransferAsset(Sender, RecipientAvatarAddress, Amount);
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary) plainValue;

            Sender = asDict["sender"].ToAddress();
            RecipientAvatarAddress = asDict["recipient"].ToAddress();
            ItemId = asDict["itemid"].ToGuid();
            Memo = asDict.TryGetValue((Text) "memo", out IValue memo) ? memo.ToDotnetString() : null;

            CheckMemoLength(Memo);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("serialized", new Codec().Encode(PlainValue));
        }

        private void CheckMemoLength(string memo)
        {
            if (memo?.Length > MemoMaxLength)
            {
                string msg = $"The length of the memo, {memo.Length}, " +
                             $"is overflowed than the max length, {MemoMaxLength}.";
                throw new MemoLengthOverflowException(msg);
            }
        }
    }
}
