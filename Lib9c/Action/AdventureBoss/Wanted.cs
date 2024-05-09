using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Exceptions.AdventureBoss;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.Model.State;
using Nekoyume.Module;

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

            var latestSeason = states.GetLatestAdventureBossSeason();

            // Create new season
            if (latestSeason.SeasonId == 0 || latestSeason.NextStartBlockIndex <= context.BlockIndex)
            {
                var currentSeason = new SeasonInfo(Season, context.BlockIndex);
                states = states.SetSeasonInfo(currentSeason);
                states = states.SetLatestAdventureBossSeason(currentSeason);
                latestSeason = states.GetLatestAdventureBossSeason();
            }

            // Validation
            if (Season != latestSeason.SeasonId)
            {
                throw new InvalidAdventureBossSeasonException(
                    $"Given season {Season} is not latest season {latestSeason.SeasonId}"
                );
            }

            if (!Addresses.CheckAvatarAddrIsContainedInAgent(context.Signer, AvatarAddress))
            {
                throw new InvalidAddressException();
            }

            var currency = states.GetGoldCurrency();
            if (!Bounty.Currency.Equals(currency))
            {
                throw new InvalidCurrencyException("");
            }

            // TODO: Check staking level

            var balance = states.GetBalance(context.Signer, currency);
            if (balance < Bounty)
            {
                throw new InsufficientBalanceException($"{Bounty}", context.Signer, balance);
            }

            states = states.TransferAsset(context, context.Signer, Addresses.BountyBoard, Bounty);

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
