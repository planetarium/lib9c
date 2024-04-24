using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.Model.State;
using Nekoyume.Module;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class Wanted : ActionBase
    {
        public const string TypeIdentifier = "wanted";
        public const long SeasonInterval = 100;
        public FungibleAssetValue Bounty;
        public Address AvatarAddress;

        public override IValue PlainValue =>
            Dictionary.Empty
                .Add("type_id", TypeIdentifier)
                .Add("values", List.Empty
                    .Add(Bounty.Serialize())
                    .Add(AvatarAddress.Serialize()));

        public override void LoadPlainValue(IValue plainValue)
        {
            var list = (List)((Dictionary)plainValue)["values"];
            Bounty = list[0].ToFungibleAssetValue();
            AvatarAddress = list[1].ToAddress();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            if (!Addresses.CheckAvatarAddrIsContainedInAgent(context.Signer, AvatarAddress))
            {
                throw new InvalidAddressException();
            }

            var currency = states.GetGoldCurrency();
            if (!Bounty.Currency.Equals(currency))
            {
                throw new InvalidCurrencyException("");
            }

            var balance = states.GetBalance(context.Signer, currency);
            if (balance >= Bounty)
            {
                states = states.TransferAsset(context, context.Signer, Addresses.BountyBoard, Bounty);
            }
            else
            {
                throw new InsufficientBalanceException($"{Bounty}", context.Signer, balance);
            }

            var season = context.BlockIndex / SeasonInterval;
            BountyBoard bountyBoard = null;
            try
            {
                bountyBoard = states.GetBountyBoard(season);
            }
            catch (FailedLoadStateException)
            {
                bountyBoard = new BountyBoard();
            }

            bountyBoard.AddOrUpdate(AvatarAddress, Bounty);
            return states.SetBountyBoard(season, bountyBoard);
        }
    }
}
