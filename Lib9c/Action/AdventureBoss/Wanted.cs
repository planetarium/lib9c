using System;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Exceptions.AdventureBoss;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action.AdventureBoss
{
    // FIXME: This may temporary
    public struct WantedReward
    {
        public int BossId;
        public int[] FixedRewardItemIdList;
        public int[] FixedRewardFavTickerList;
        public int[] RandomRewardItemIdList;
        public int[] RandomRewardFavTickerList;
    }

    [Serializable]
    [ActionType(TypeIdentifier)]
    public class Wanted : ActionBase
    {
        public const string TypeIdentifier = "wanted";
        public const int RequiredStakingLevel = 5;
        public const int MinBounty = 100;
        public int Season;
        public FungibleAssetValue Bounty;
        public Address AvatarAddress;

        // FIXME: This may temporary
        public WantedReward[] WantedRewardList = new[]
        {
            new WantedReward
            {
                BossId = 900001,
                FixedRewardItemIdList = new[] { 600201 },
                FixedRewardFavTickerList = Array.Empty<int>(),
                RandomRewardItemIdList = Array.Empty<int>(),
                RandomRewardFavTickerList = new[] { 20001, 30001 },
            },
            new WantedReward
            {
                BossId = 900002,
                FixedRewardItemIdList = new[] { 600202 },
                FixedRewardFavTickerList = Array.Empty<int>(),
                RandomRewardItemIdList = Array.Empty<int>(),
                RandomRewardFavTickerList = new[] { 20001, 30001 },
            },
            new WantedReward
            {
                BossId = 900001,
                FixedRewardItemIdList = Array.Empty<int>(),
                FixedRewardFavTickerList = new[] { 20001, 30001 },
                RandomRewardItemIdList = new[] { 600201, 600202, 600203 },
                RandomRewardFavTickerList = Array.Empty<int>(),
            },
            new WantedReward
            {
                BossId = 900002,
                FixedRewardItemIdList = new[] { 600202 },
                FixedRewardFavTickerList = Array.Empty<int>(),
                RandomRewardItemIdList = new[] { 600201, 600202, 600203 },
                RandomRewardFavTickerList = Array.Empty<int>(),
            },
        };

        public override IValue PlainValue =>
            Dictionary.Empty
                .Add("type_id", TypeIdentifier)
                .Add("values", List.Empty
                    .Add(Season.Serialize())
                    .Add(Bounty.Serialize())
                    .Add(AvatarAddress.Serialize()));

        public override void LoadPlainValue(IValue plainValue)
        {
            var list = (List)((Dictionary)plainValue)["values"];
            Season = list[0].ToInteger();
            Bounty = list[1].ToFungibleAssetValue();
            AvatarAddress = list[2].ToAddress();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            var currency = states.GetGoldCurrency();

            var latestSeason = states.GetLatestAdventureBossSeason();

            // Validation
            if (!Bounty.Currency.Equals(currency))
            {
                throw new InvalidCurrencyException("");
            }

            if (Bounty < MinBounty * currency)
            {
                throw new InvalidBountyException(
                    $"Given bounty {Bounty.MajorUnit}.{Bounty.MinorUnit} is less than {MinBounty}");
            }

            var balance = states.GetBalance(context.Signer, currency);
            if (balance < Bounty)
            {
                throw new InsufficientBalanceException($"{Bounty}", context.Signer, balance);
            }

            if (Season <= 0 ||
                Season > latestSeason.SeasonId + 1 || Season < latestSeason.SeasonId ||
                (Season == latestSeason.SeasonId && context.BlockIndex > latestSeason.EndBlockIndex) ||
                (Season == latestSeason.SeasonId + 1 && context.BlockIndex < latestSeason.NextStartBlockIndex)
               )
            {
                throw new InvalidAdventureBossSeasonException(
                    $"Given season {Season} is not valid season."
                );
            }

            // Cannot put bounty in two seasons in a row
            if (Season > 1)
            {
                var prevBountyBoard = states.GetBountyBoard(Season - 1);
                if (prevBountyBoard.Investors.Select(i => i.AvatarAddress).Contains(AvatarAddress))
                {
                    throw new PreviousBountyException(
                        "You've put bounty in previous season. Cannot put bounty tow seasons in a row"
                    );
                }
            }

            if (!Addresses.CheckAvatarAddrIsContainedInAgent(context.Signer, AvatarAddress))
            {
                throw new InvalidAddressException();
            }

            var requiredStakingAmount = states.GetSheet<MonsterCollectionSheet>()
                .OrderedList.First(row => row.Level == RequiredStakingLevel).RequiredGold;
            var stakedAmount =
                states.GetStakedAmount(states.GetAvatarState(AvatarAddress).agentAddress);
            if (stakedAmount < requiredStakingAmount * currency)
            {
                throw new InsufficientStakingException(
                    $"Current staking {stakedAmount.MajorUnit} is not enough: requires {requiredStakingAmount}"
                );
            }

            BountyBoard bountyBoard;
            // Create new season if required
            if (latestSeason.SeasonId == 0 ||
                latestSeason.NextStartBlockIndex <= context.BlockIndex)
            {
                var seasonInfo = new SeasonInfo(Season, context.BlockIndex);
                bountyBoard = new BountyBoard(Season);

                // Set season info: boss and reward
                var random = context.GetRandom();
                var wantedReward = WantedRewardList[random.Next(0, WantedRewardList.Length)];
                seasonInfo.BossId = wantedReward.BossId;
                bountyBoard.SetReward(wantedReward, random);
                states = states.SetSeasonInfo(seasonInfo);
                states = states.SetLatestAdventureBossSeason(seasonInfo);
                states = states.SetBountyBoard(Season, bountyBoard);
            }

            // Just update bounty board
            else
            {
                bountyBoard = states.GetBountyBoard(Season);
            }

            // FIXME: Send bounty to seasonal board
            states = states.TransferAsset(context, context.Signer, Addresses.BountyBoard, Bounty);
            bountyBoard.AddOrUpdate(AvatarAddress, Bounty);
            return states.SetBountyBoard(Season, bountyBoard);
        }
    }
}