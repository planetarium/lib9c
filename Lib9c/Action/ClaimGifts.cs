using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// An action to claim gifts using the Gift Id. <br/>
    /// Refer <see cref="ClaimableGiftsSheet"/> to find gifts data.<br/>
    /// It can only claim once at the specified block index.
    /// </summary>
    [ActionType(ActionTypeText)]
    public class ClaimGifts : GameAction
    {
        private const string ActionTypeText = "claim_gifts";

        /// <summary>
        /// The address of the avatar claiming the gift.
        /// </summary>
        public Address AvatarAddress;
        /// <summary>
        /// The ID of the gift to be claimed. This ID is used in the <see cref="ClaimableGiftsSheet"/>.
        /// </summary>
        public int GiftId;
        private const string GiftIdKey = "gi";

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add(AvatarAddressKey, AvatarAddress.Serialize())
                .Add(GiftIdKey, GiftId.Serialize());

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
            GiftId = plainValue[GiftIdKey].ToInteger();
        }

        /// <exception cref="FailedLoadStateException">Thrown when the inventory could not be loaded.</exception>
        /// <exception cref="SheetRowNotFoundException">Thrown when the gift ID is not found in the <see cref="ClaimableGiftsSheet"/>.</exception>
        /// <exception cref="ClaimableGiftsNotAvailableException">Thrown when the gift is not available at the current block index.</exception>
        /// <exception cref="AlreadyClaimedGiftsException">Thrown when the gift has already been claimed.</exception>
        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            var random = context.GetRandom();
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);

            var inventory = states.GetInventoryV2(AvatarAddress);
            if (inventory is null)
            {
                throw new FailedLoadStateException(
                    ActionTypeText,
                    addressesHex,
                    typeof(Inventory),
                    AvatarAddress);
            }

            var sheetTypes = new []
            {
                typeof(ClaimableGiftsSheet),
            };
            var sheets = states.GetSheets(
                containItemSheet: true,
                sheetTypes: sheetTypes);

            var claimableGiftsSheet = sheets.GetSheet<ClaimableGiftsSheet>();
            if (!claimableGiftsSheet.TryGetValue(GiftId, out var giftRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(claimableGiftsSheet),
                    GiftId);
            }

            if (!giftRow.Validate(context.BlockIndex))
            {
                throw new ClaimableGiftsNotAvailableException(
                    $"[{addressesHex}] Claimable gift is not available at block index: {context.BlockIndex}"
                );
            }

            var claimedGiftIds = states.GetClaimedGifts(AvatarAddress);
            if (claimedGiftIds.Contains(giftRow.Id))
            {
                throw new AlreadyClaimedGiftsException(
                    $"[{addressesHex}] Already claimed gift. You can only claim gift once : {giftRow.Id}"
                );
            }

            var itemSheet = sheets.GetItemSheet();
            foreach (var (itemId, quantity) in giftRow.Items)
            {
                var item = ItemFactory.CreateItem(itemSheet[itemId], random);
                if (item is INonFungibleItem)
                {
                    foreach (var _ in Enumerable.Range(0, quantity))
                    {
                        inventory.AddItem(item);
                    }
                }
                else
                {
                    inventory.AddItem(item, quantity);
                }
            }

            claimedGiftIds.Add(giftRow.Id);

            return states
                .SetClaimedGifts(AvatarAddress, claimedGiftIds)
                .SetInventory(AvatarAddress, inventory);
        }
    }
}
