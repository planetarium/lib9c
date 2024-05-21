using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Exceptions.AdventureBoss;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action.AdventureBoss
{
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
            var random = context.GetRandom();
            var bountyBoard = states.GetBountyBoard(Season);
            if (bountyBoard.RaffleWinner is null)
            {
                bountyBoard.RaffleReward = (bountyBoard.totalBounty() * 5).DivRem(100, out _);
                var totalProb = bountyBoard.Investors.Aggregate(new BigInteger(0),
                    (current, inv) => current + inv.Price.RawValue);
                var target = (BigInteger)random.Next((int)totalProb);
                foreach (var inv in bountyBoard.Investors)
                {
                    if (target < inv.Price.RawValue)
                    {
                        bountyBoard.RaffleWinner = inv.AvatarAddress;
                        break;
                    }

                    target -= inv.Price.RawValue;
                }

                states = states.SetBountyBoard(Season, bountyBoard);
            }

            var exploreBoard = states.GetExploreBoard(Season);
            if (exploreBoard.RaffleWinner is null)
            {
                exploreBoard.RaffleReward = (bountyBoard.totalBounty() * 5).DivRem(100, out _);
                if (exploreBoard.ExplorerList.Count > 0)
                {
                    exploreBoard.RaffleWinner = exploreBoard.ExplorerList.ToImmutableSortedSet()[
                        random.Next(exploreBoard.ExplorerList.Count)
                    ];
                }
                else
                {
                    exploreBoard.RaffleWinner = new Address();
                }

                states = states.SetExploreBoard(Season, exploreBoard);
            }

            // Send 75% NCG to operational account. 25% are for rewards.
            states = states.TransferAsset(context,
                Addresses.BountyBoard.Derive(AdventureBossHelper.GetSeasonAsAddressForm(Season)),
                // FIXME: Set operational account address
                new Address(), (bountyBoard.totalBounty() * 75).DivRem(100, out _)
            );

            // Collect wanted reward
            var currentBlockIndex = context.BlockIndex;
            var myReward = new ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>(),
            };
            states = AdventureBossHelper.CollectWantedReward(
                states, myReward, currentBlockIndex, Season, AvatarAddress, out myReward
            );

            // Collect explore reward
            states = AdventureBossHelper.CollectExploreReward(
                states, myReward, currentBlockIndex, Season, AvatarAddress, out myReward
            );

            // Give rewards
            if (myReward.NcgReward is not null)
            {
                var avatarState = states.GetAvatarState(AvatarAddress);
                states = states.TransferAsset(context,
                    Addresses.BountyBoard.Derive(
                        AdventureBossHelper.GetSeasonAsAddressForm(Season)
                    ),
                    avatarState.agentAddress,
                    (FungibleAssetValue)myReward.NcgReward
                );
            }

            if (myReward.ItemReward.Count > 0)
            {
                var materialSheet = states.GetSheet<MaterialItemSheet>();
                var inventory = states.GetInventory(AvatarAddress);
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
