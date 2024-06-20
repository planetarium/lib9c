using System;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Exceptions.AdventureBoss;
using Nekoyume.Exceptions;
using Nekoyume.Helper;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.AdventureBoss;
using Nekoyume.TableData.Stake;

namespace Nekoyume.Action.AdventureBoss
{
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class Wanted : ActionBase
    {
        public const string TypeIdentifier = "wanted";

        public int Season;
        public FungibleAssetValue Bounty;
        public Address AvatarAddress;

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
            var gameConfig = states.GetGameConfigState();
            var latestSeason = states.GetLatestAdventureBossSeason();

            // Validation
            if (!Bounty.Currency.Equals(currency))
            {
                throw new InvalidCurrencyException("");
            }

            if (Bounty < gameConfig.AdventureBossMinBounty * currency)
            {
                throw new InvalidBountyException(
                    $"Given bounty {Bounty.MajorUnit}.{Bounty.MinorUnit} is less than {gameConfig.AdventureBossMinBounty}");
            }

            var balance = states.GetBalance(context.Signer, currency);
            if (balance < Bounty)
            {
                throw new InsufficientBalanceException($"{Bounty}", context.Signer, balance);
            }

            if (Season <= 0 ||
                Season > latestSeason.Season + 1 || Season < latestSeason.Season ||
                (Season == latestSeason.Season &&
                 context.BlockIndex > latestSeason.EndBlockIndex) ||
                (Season == latestSeason.Season + 1 &&
                 context.BlockIndex < latestSeason.NextStartBlockIndex)
               )
            {
                throw new InvalidAdventureBossSeasonException(
                    $"Given season {Season} is not valid season."
                );
            }

            if (!Addresses.CheckAvatarAddrIsContainedInAgent(context.Signer, AvatarAddress))
            {
                throw new InvalidAddressException();
            }

            var requiredStakingLevel =
                states.GetGameConfigState().AdventureBossWantedRequiredStakingLevel;
            var currentStakeRegularRewardSheetAddr = Addresses.GetSheetAddress(
                states.GetSheet<StakePolicySheet>().StakeRegularRewardSheetValue);
            if (!states.TryGetSheet<StakeRegularRewardSheet>(
                    currentStakeRegularRewardSheetAddr,
                    out var stakeRegularRewardSheet))
            {
                throw new StateNullException(ReservedAddresses.LegacyAccount,
                    currentStakeRegularRewardSheetAddr);
            }

            var requiredStakingAmount = stakeRegularRewardSheet[requiredStakingLevel].RequiredGold;
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
            if (latestSeason.Season == 0 ||
                latestSeason.NextStartBlockIndex <= context.BlockIndex)
            {
                var seasonInfo = new SeasonInfo(Season, context.BlockIndex,
                    gameConfig.AdventureBossActiveInterval,
                    gameConfig.AdventureBossInactiveInterval);
                bountyBoard = new BountyBoard(Season);
                var exploreBoard = new ExploreBoard(Season);
                var explorerList = new ExplorerList(Season);

                // Set season info: boss and reward
                var random = context.GetRandom();
                var adventureBossSheet = states.GetSheet<AdventureBossSheet>();
                var boss = adventureBossSheet.OrderedList[
                    random.Next(0, adventureBossSheet.Values.Count)
                ];
                seasonInfo.BossId = boss.BossId;

                var wantedReward = states.GetSheet<AdventureBossWantedRewardSheet>()
                    .OrderedList.First(row => row.AdventureBossId == boss.Id);
                bountyBoard.SetReward(wantedReward, random);

                var contribReward = states.GetSheet<AdventureBossContributionRewardSheet>()
                    .OrderedList.First(row => row.AdventureBossId == boss.Id);
                exploreBoard.SetReward(contribReward, random);

                states = states.SetSeasonInfo(seasonInfo)
                    .SetLatestAdventureBossSeason(seasonInfo)
                    .SetBountyBoard(Season, bountyBoard)
                    .SetExploreBoard(Season, exploreBoard)
                    .SetExplorerList(Season, explorerList);
            }

            // Just update bounty board
            else
            {
                bountyBoard = states.GetBountyBoard(Season);
            }

            // FIXME: Send bounty to seasonal board
            states = states.TransferAsset(context, context.Signer,
                Addresses.BountyBoard.Derive(AdventureBossHelper.GetSeasonAsAddressForm(Season)),
                Bounty);
            bountyBoard.AddOrUpdate(AvatarAddress, states.GetAvatarState(AvatarAddress).name,
                Bounty);
            return states.SetBountyBoard(Season, bountyBoard);
        }
    }
}
