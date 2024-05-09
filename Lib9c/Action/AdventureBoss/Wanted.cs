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
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class Wanted : ActionBase
    {
        public const string TypeIdentifier = "wanted";
        public const int RequiredStakingLevel = 5;
        public const int MinBounty = 100;
        public const int MaxBounty = 1000;
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

            var latestSeason = states.GetLatestAdventureBossSeason();

            // Validation
            if (!Bounty.Currency.Equals(currency))
            {
                throw new InvalidCurrencyException("");
            }

            if (Bounty < MinBounty * currency || Bounty > MaxBounty * currency)
            {
                throw new InvalidBountyException(
                    $"Given bounty {Bounty.RawValue} is not between {MinBounty} and {MaxBounty}.");
            }

            if (Season == 0 || Season != latestSeason.SeasonId)
            {
                throw new InvalidAdventureBossSeasonException(
                    $"Given season {Season} is not latest season {latestSeason.SeasonId}"
                );
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

            // Create new season if required
            SeasonInfo currentSeason;
            if (latestSeason.SeasonId == 0 ||
                latestSeason.NextStartBlockIndex <= context.BlockIndex)
            {
                currentSeason = new SeasonInfo(Season, context.BlockIndex);
                states = states.SetSeasonInfo(currentSeason);
                states = states.SetLatestAdventureBossSeason(currentSeason);
                states.GetLatestAdventureBossSeason();
            }
            else
            {
                currentSeason = states.GetSeasonInfo(Season);
            }

            // Check balance and use
            var balance = states.GetBalance(context.Signer, currency);
            if (balance < Bounty)
            {
                throw new InsufficientBalanceException($"{Bounty}", context.Signer, balance);
            }

            states = states.TransferAsset(context, context.Signer, Addresses.BountyBoard, Bounty);

            // Update Bounty board
            BountyBoard bountyBoard;
            try
            {
                bountyBoard = states.GetBountyBoard(Season);
            }
            catch (FailedLoadStateException)
            {
                bountyBoard = new BountyBoard();
            }

            bountyBoard.AddOrUpdate(AvatarAddress, Bounty);
            return states.SetBountyBoard(Season, bountyBoard);
        }
    }
}
