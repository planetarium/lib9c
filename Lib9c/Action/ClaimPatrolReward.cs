using System;
using System.Collections.Generic;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;

namespace Nekoyume.Action
{
    /// <summary>
    /// Claim patrol reward
    /// </summary>
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class ClaimPatrolReward : ActionBase
    {
        public const string TypeIdentifier = "claim_patrol_reward";
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
            if (claimedBlockIndex > 0L && claimedBlockIndex + row.Interval > context.BlockIndex)
            {
                throw new RequiredBlockIndexException();
            }

            // mit rewards
            var random = context.GetRandom();
            var favs = new List<FungibleAssetValue>();
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
                    var fav = currency * reward.Count;
                    states = states.MintAsset(context, recipient, fav);
                    favs.Add(fav);
                }
            }

            var mailBox = avatarState.mailBox;
            var mail = new PatrolRewardMail(context.BlockIndex, random.GenerateRandomGuid(), context.BlockIndex, favs, items);
            mailBox.Add(mail);
            mailBox.CleanUp();
            avatarState.mailBox = mailBox;

            // set states
            states = states
                .SetAvatarState(AvatarAddress, avatarState, setAvatar: true, setInventory: true, setWorldInformation: false, setQuestList: false)
                .SetPatrolRewardClaimedBlockIndex(AvatarAddress, context.BlockIndex);
            return states;
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
