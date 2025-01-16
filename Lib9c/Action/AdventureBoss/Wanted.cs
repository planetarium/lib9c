using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using Nekoyume.Module.Guild;
using Nekoyume.TableData;
using Nekoyume.TableData.AdventureBoss;
using Nekoyume.TableData.Stake;

namespace Nekoyume.Action.AdventureBoss
{
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class Wanted : GameAction
    {
        public const string TypeIdentifier = "wanted";

        public int Season;
        public FungibleAssetValue Bounty;
        public Address AvatarAddress;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["s"] = (Integer)Season,
                ["b"] = Bounty.Serialize(),
                ["a"] = AvatarAddress.Serialize()
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue
        )
        {
            Season = (Integer)plainValue["s"];
            Bounty = plainValue["b"].ToFungibleAssetValue();
            AvatarAddress = plainValue["a"].ToAddress();
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            var currency = states.GetGoldCurrency();
            var gameConfig = states.GetGameConfigState();
            var latestSeason = states.GetLatestAdventureBossSeason();

            // Validation
            // Only NCG allowed
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

            // Ignore invalid seasons including duplication and too future
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

            // Min. required staking level exists to add bounty
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
            
            var avatarState = states.GetAvatarState(AvatarAddress, false, false, false);
            if (avatarState is null || !avatarState.agentAddress.Equals(context.Signer))
            {
                var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }
            
            var stakedAmount = states.GetStaked(avatarState.agentAddress);
            if (stakedAmount < requiredStakingAmount * currency)
            {
                throw new InsufficientStakingException(
                    $"Current staking {stakedAmount.MajorUnit} is not enough: requires {requiredStakingAmount}"
                );
            }

            BountyBoard bountyBoard;
            // Create new season if required
            if (latestSeason.Season == 0 || latestSeason.NextStartBlockIndex <= context.BlockIndex)
            {
                var seasonInfo = new SeasonInfo(Season, context.BlockIndex,
                    gameConfig.AdventureBossActiveInterval,
                    gameConfig.AdventureBossInactiveInterval);
                bountyBoard = new BountyBoard(Season);
                var exploreBoard = new ExploreBoard(Season);
                var explorerList = new ExplorerList(Season);

                // Set season info: boss and reward
                var random = context.GetRandom();

                // latestSeason is last season. Check latest-1 season to get second last season
                var prevSeason = new SeasonInfo(0, 0, 0, 0) { BossId = 0 };
                if (latestSeason.Season > 1)
                {
                    prevSeason = states.GetSeasonInfo(latestSeason.Season - 1);
                }

                var adventureBossSheet = states.GetSheet<AdventureBossSheet>();
                var candidate = adventureBossSheet.OrderedList.Where(
                    row => row.BossId != latestSeason.BossId && row.BossId != prevSeason.BossId
                ).ToList();
                var boss = candidate[random.Next(candidate.Count)];
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
            bountyBoard.AddOrUpdate(AvatarAddress, avatarState.name,
                Bounty);
            return states.SetBountyBoard(Season, bountyBoard);
        }
    }
}
