using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("redeem_code3")]
    public class RedeemCode : GameAction
    {
        public string Code { get; internal set; }

        public Address AvatarAddress { get; internal set; }

        public RedeemCode()
        {
        }

        public RedeemCode(string code, Address avatarAddress)
        {
            Code = code;
            AvatarAddress = avatarAddress;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            Address inventoryAddress = AvatarAddress.Derive(LegacyInventoryKey);
            Address worldInformationAddress = AvatarAddress.Derive(LegacyWorldInformationKey);
            Address questListAddress = AvatarAddress.Derive(LegacyQuestListKey);
            if (context.Rehearsal)
            {
                return states
                    .SetState(RedeemCodeState.Address, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(AvatarAddress, MarkChanged)
                    .SetState(context.Signer, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, GoldCurrencyState.Address)
                    .MarkBalanceChanged(GoldCurrencyMock, context.Signer);
            }

            string addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);

            if (!states.TryGetAvatarStateV2(context.Signer, AvatarAddress, out AvatarState avatarState))
            {
                return states;
            }

            RedeemCodeState redeemState = states.GetRedeemCodeState();
            if (redeemState is null)
            {
                return states;
            }

            int redeemId;
            try
            {
                redeemId = redeemState.Redeem(Code, AvatarAddress);
            }
            catch (InvalidRedeemCodeException)
            {
                Log.Error("{AddressesHex}Invalid Code", addressesHex);
                throw;
            }
            catch (DuplicateRedeemException e)
            {
                Log.Warning("{AddressesHex}{Message}", addressesHex, e.Message);
                throw;
            }

            RedeemRewardSheet.Row row = states.GetSheet<RedeemRewardSheet>().Values.First(r => r.Id == redeemId);
            ItemSheet itemSheets = states.GetItemSheet();

            foreach (RedeemRewardSheet.RewardInfo info in row.Rewards)
            {
                switch (info.Type)
                {
                    case RewardType.Item:
                        if (info.ItemId is int itemId)
                        {
                            ItemSheet.Row itemRow = itemSheets[itemId];

                            switch (itemRow.ItemType)
                            {
                                case ItemType.Costume:
                                case ItemType.Equipment:
                                    AddNonFungibleItems(info, itemRow, context.Random, avatarState);
                                    break;
                                case ItemType.Material:
                                    Material material = ItemFactory.CreateMaterial((MaterialItemSheet.Row)itemRow);
                                    avatarState.inventory.AddFungibleItem(material, info.Quantity);
                                    break;
                                default:
                                    // FIXME: We should raise exception here.
                                    break;
                            }
                        }
                        break;
                    case RewardType.Gold:
                        states = states.TransferAsset(
                            GoldCurrencyState.Address,
                            context.Signer,
                            states.GetGoldCurrency() * info.Quantity
                        );
                        break;
                    default:
                        // FIXME: We should raise exception here.
                        break;
                }
            }
            return states
                .SetState(AvatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(RedeemCodeState.Address, redeemState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [nameof(Code)] = Code.Serialize(),
                [nameof(AvatarAddress)] = AvatarAddress.Serialize()
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            Code = (Text)plainValue[nameof(Code)];
            AvatarAddress = plainValue[nameof(AvatarAddress)].ToAddress();
        }

        private void AddNonFungibleItems(
            RedeemRewardSheet.RewardInfo info,
            ItemSheet.Row row,
            IRandom random,
            AvatarState avatarState)
        {
            for (int i = 0; i < info.Quantity; i++)
            {
                ItemBase nonFungibleItem = ItemFactory.CreateItem(row, random);
                // We should fix count as 1 because
                // ItemFactory.CreateItem will create a new item every time.
                _ = avatarState.inventory.AddItem(nonFungibleItem, count: 1);
            }
        }
    }
}
