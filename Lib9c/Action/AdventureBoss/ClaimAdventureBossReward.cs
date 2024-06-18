using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Exceptions.AdventureBoss;
using Nekoyume.Data;
using Nekoyume.Exceptions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action.AdventureBoss
{
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class ClaimAdventureBossReward : ActionBase
    {
        public const string TypeIdentifier = "claim_adventure_boss_reward";
        public const long ClaimableDuration = 100_000L;

        public long Season;
        public Address AvatarAddress;

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", List.Empty
                .Add(Season.Serialize())
                .Add(AvatarAddress.Serialize())
            );

        public override void LoadPlainValue(IValue plainValue)
        {
            var values = (List)((Dictionary)plainValue)["values"];
            Season = values[0].ToInteger();
            AvatarAddress = values[1].ToAddress();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;

            // Validation
            var addresses = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            // NOTE: The `AvatarAddress` must contained in `Signer`'s `AgentState.avatarAddresses`.
            if (!Addresses.CheckAvatarAddrIsContainedInAgent(context.Signer, AvatarAddress))
            {
                throw new InvalidActionFieldException(
                    TypeIdentifier,
                    addresses,
                    nameof(AvatarAddress),
                    $"Signer({context.Signer}) is not contained in" +
                    $" AvatarAddress({AvatarAddress}).");
            }

            var latestSeason = states.GetLatestAdventureBossSeason();
            if (Season > latestSeason.Season)
            {
                throw new InvalidAdventureBossSeasonException(
                    $"Given season {Season} is not valid.");
            }

            var seasonInfo = states.GetSeasonInfo(Season);
            if (seasonInfo.EndBlockIndex > context.BlockIndex)
            {
                throw new SeasonInProgressException(
                    $"Adventure boss season {Season} will be finished at {seasonInfo.EndBlockIndex}: current block is {context.BlockIndex}"
                );
            }

            if (seasonInfo.EndBlockIndex + ClaimableDuration < context.BlockIndex)
            {
                throw new ClaimExpiredException(
                    $"Claim expired at block {seasonInfo.EndBlockIndex + ClaimableDuration}: current block index is {context.BlockIndex}"
                );
            }

            // Pick raffle winner if not exists
            states = AdventureBossHelper.PickRaffleWinner(states, context, Season);

            // Send 80% NCG to operational account. 20% are for rewards.
            var bountyBoard = states.GetBountyBoard(Season);
            states = states.TransferAsset(context,
                Addresses.BountyBoard.Derive(AdventureBossHelper.GetSeasonAsAddressForm(Season)),
                // FIXME: Set operational account address
                new Address(), (bountyBoard.totalBounty() * 80).DivRem(100, out _)
            );

            var currentBlockIndex = context.BlockIndex;
            var myReward = new AdventureBossGameData.ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>(),
            };

            // Collect wanted reward
            states = AdventureBossHelper.CollectWantedReward(
                states, context, myReward, currentBlockIndex, Season, context.Signer, AvatarAddress, out myReward
            );

            // Collect explore reward
            states = AdventureBossHelper.CollectExploreReward(
                states, context, myReward, currentBlockIndex, Season, context.Signer, AvatarAddress, out myReward
            );

            // Give rewards
            // NOTE: NCG must be transferred from seasonal address. So this must be done in collection stage.
            if (myReward.ItemReward.Count > 0)
            {
                var materialSheet = states.GetSheet<MaterialItemSheet>();
                var inventory = states.GetInventoryV2(AvatarAddress);
                foreach (var reward in myReward.ItemReward.ToImmutableSortedDictionary())
                {
                    var material =
                        ItemFactory.CreateMaterial(
                            materialSheet.Values.First(row => row.Id == reward.Key));
                    inventory.AddItem(material, reward.Value);
                }

                states = states.SetInventory(AvatarAddress, inventory);
            }

            if (myReward.FavReward.Count > 0)
            {
                var runeSheet = states.GetSheet<RuneSheet>();
                foreach (var reward in myReward.FavReward.ToImmutableSortedDictionary())
                {
                    var runeRow = runeSheet.Values.First(row => row.Id == reward.Key);
                    var ticker = runeRow.Ticker;
                    var currency = Currencies.GetRune(ticker);
                    states = states.MintAsset(context, AvatarAddress, currency * reward.Value);
                }
            }

            return states;
        }
    }
}
