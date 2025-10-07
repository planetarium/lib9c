using System;
using System.Collections.Generic;
using Bencodex.Types;
using Lib9c.Extensions;
using Lib9c.Helper;
using Lib9c.Model.Mail;
using Lib9c.Model.State;
using Lib9c.Module;
using Lib9c.TableData.Event;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Action
{
    /// <summary>
    /// Claim patrol reward
    /// </summary>
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class ClaimPatrolReward : ActionBase
    {
        public const string TypeIdentifier = "claim_patrol_reward";

        /// <summary>
        /// The address of the avatar to receive the patrol reward.
        /// </summary>
        public Address AvatarAddress;

        public ClaimPatrolReward()
        {
        }

        public ClaimPatrolReward(Address avatarAddress)
        {
            AvatarAddress = avatarAddress;
        }
        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var signer = context.Signer;
            var states = context.PreviousState;

            // Validate that the avatar address belongs to the signer.
            // This ensures that only the owner of the avatar can claim the patrol reward for it.
            if (!Addresses.CheckAvatarAddrIsContainedInAgent(signer, AvatarAddress))
            {
                throw new InvalidAddressException();
            }

            // avatar
            var avatarState = states.GetAvatarState(AvatarAddress, true, false, false);
            var avatarLevel = avatarState.level;
            var inventory = avatarState.inventory;

            // sheets
            var sheets = states.GetSheets(containItemSheet: true, sheetTypes: new[]
            {
                typeof(PatrolRewardSheet)
            });
            var patrolRewardSheet = sheets.GetSheet<PatrolRewardSheet>();
            var itemSheet = sheets.GetItemSheet();

            // validate
            states.TryGetPatrolRewardClaimedBlockIndex(AvatarAddress, out var claimedBlockIndex);
            var row = patrolRewardSheet.FindByLevel(avatarLevel, context.BlockIndex);

            // Ensure rewards cannot be claimed too frequently.
            // If the last claimed block index is set and the current block index is less than the allowed interval, throw an exception.
            if (claimedBlockIndex > 0L && claimedBlockIndex + row.Interval > context.BlockIndex)
            {
                throw new RequiredBlockIndexException();
            }

            // mint rewards
            var random = context.GetRandom();
            var fav = new List<FungibleAssetValue>();
            var items = new List<(int id, int count)>();
            foreach (var reward in row.Rewards)
            {
                var ticker = reward.Ticker;
                if (string.IsNullOrEmpty(ticker))
                {
                    var itemRow = itemSheet[reward.ItemId];
                    inventory.MintItem(itemRow, reward.Count, false, random);
                    items.Add(new (reward.ItemId, reward.Count));
                }
                else
                {
                    var currency = Currencies.GetMinterlessCurrency(ticker);
                    var recipient = Currencies.PickAddress(currency, signer, AvatarAddress);
                    var asset = currency * reward.Count;
                    states = states.MintAsset(context, recipient, asset);
                    fav.Add(asset);
                }
            }

            var mailBox = avatarState.mailBox;
            var mail = new PatrolRewardMail(context.BlockIndex, random.GenerateRandomGuid(), context.BlockIndex, fav, items);
            mailBox.Add(mail);
            mailBox.CleanUp();
            avatarState.mailBox = mailBox;

            // set states
            return states
                .SetAvatarState(AvatarAddress, avatarState, setAvatar: true, setInventory: true, setWorldInformation: false, setQuestList: false)
                .SetPatrolRewardClaimedBlockIndex(AvatarAddress, context.BlockIndex);
        }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", AvatarAddress.Serialize());
        public override void LoadPlainValue(IValue plainValue)
        {
            AvatarAddress = ((Dictionary)plainValue)["values"].ToAddress();
        }
    }
}
