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
using static Lib9c.SerializeKeys;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

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
        private const int transferFee = 10;
        

        public TransferItem()
        {
        }

        public TransferItem(Address sender, Address recipient, Guid itemId, string memo = null)
        {
            SenderAvatarAddress = sender;
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

        public Address SenderAvatarAddress { get; private set; }
        public Address RecipientAvatarAddress { get; private set; }
        public Guid ItemId;
        public string Memo { get; private set; }

        public override IValue PlainValue
        {
            get
            {
                IEnumerable<KeyValuePair<IKey, IValue>> pairs = new[]
                {
                    new KeyValuePair<IKey, IValue>((Text) "sender", SenderAvatarAddress.Serialize()),
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
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            if (SenderAvatarAddress != context.Signer)
            {
                throw new InvalidTransferSignerException(context.Signer, SenderAvatarAddress, RecipientAvatarAddress);
            }

            Address recipientAddress = RecipientAvatarAddress.Derive(ActivationKey.DeriveKey);

            // Check new type of activation first.
            if (states.GetState(recipientAddress) is null && states.GetState(Addresses.ActivatedAccount) is Dictionary asDict )
            {
                var activatedAccountsState = new ActivatedAccountsState(asDict);
                var activatedAccounts = activatedAccountsState.Accounts;
                // if ActivatedAccountsState is empty, all user is activate.
                if (activatedAccounts.Count != 0
                    && !activatedAccounts.Contains(RecipientAvatarAddress))
                {
                    throw new InvalidTransferUnactivatedRecipientException(SenderAvatarAddress, RecipientAvatarAddress);
                }
            }
            var recipientInventoryAddress = RecipientAvatarAddress.Derive(LegacyInventoryKey);
            var recipientWorldInformationAddress = RecipientAvatarAddress.Derive(LegacyWorldInformationKey);
            var recipientQuestListAddress = RecipientAvatarAddress.Derive(LegacyQuestListKey);
            var senderInventoryAddress = SenderAvatarAddress.Derive(LegacyInventoryKey);
            var addressesHex = GetSignerAndOtherAddressesHex(context, RecipientAvatarAddress);
            AvatarState senderAvatarState;
            try
            {
                senderAvatarState = states.GetAvatarStateV2(SenderAvatarAddress);
            }
            // BackWard compatible.
            catch (FailedLoadStateException)
            {
                senderAvatarState = states.GetAvatarState(SenderAvatarAddress);
            }
            if (senderAvatarState is null)
            {
                throw new FailedLoadStateException(
                    $"Aborted as the avatar state of the sender ({senderAvatarState}) was failed to load.");
            }

            AvatarState recipientAvatarState;
            try
            {
                recipientAvatarState = states.GetAvatarStateV2(RecipientAvatarAddress);
            }
            // BackWard compatible.
            catch (FailedLoadStateException)
            {
                recipientAvatarState = states.GetAvatarState(RecipientAvatarAddress);
            }
            if (recipientAvatarState is null)
            {
                throw new FailedLoadStateException(
                    $"Aborted as the avatar state of the sender ({recipientAvatarState}) was failed to load.");
            }

            if (!recipientAvatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                recipientAvatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(addressesHex,
                    GameConfig.RequireClearedStageLevel.ActionsInShop, current);
            }

            if (!senderAvatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.ActionsInShop))
            {
                senderAvatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(addressesHex,
                    GameConfig.RequireClearedStageLevel.ActionsInShop, current);
            }

            if(senderAvatarState.agentAddress != context.Signer)
            {
                throw new InvalidAddressException("Signer doesn't match sending agent address");
            }

            if(!senderAvatarState.inventory.TryGetTradableItem(ItemId,context.BlockIndex, 1, out var item))
            {
                throw new InvalidAddressException("Unable to get item from inventory");
            }
            if (item.Locked)
            {
                throw new Exception("Item is current locked, unable to send while on the market");
            }
            if(item is INonFungibleItem nonFungibleItem)
            {
                nonFungibleItem.RequiredBlockIndex = context.BlockIndex;
                senderAvatarState.inventory.RemoveNonFungibleItem(nonFungibleItem);
                if (nonFungibleItem is Costume costume)
                {
                    recipientAvatarState.UpdateFromAddCostume(costume, false);
                }
                else
                {
                    recipientAvatarState.UpdateFromAddItem((ItemUsable)nonFungibleItem, false);
                }
                //Transfer fee
                var arenaSheet = states.GetSheet<ArenaSheet>();
                var arenaData = arenaSheet.GetRoundByBlockIndex(context.BlockIndex);
                var feeStoreAddress = Addresses.GetShopFeeAddress(arenaData.ChampionshipId, arenaData.Round);
                var goldCurrency = states.GetGoldCurrency();
                var fee = transferFee * goldCurrency;
                states = states.TransferAsset(context.Signer, feeStoreAddress, fee);
            }
            else
            {
                throw new InvalidItemTypeException("Unable to load item to send");
            }
            states = states
                .SetState(recipientInventoryAddress, recipientAvatarState.inventory.Serialize())
                .SetState(senderInventoryAddress, senderAvatarState.inventory.Serialize());
            return states;//.TransferAsset(Sender, RecipientAvatarAddress, Amount);
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary) plainValue;

            SenderAvatarAddress = asDict["sender"].ToAddress();
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
