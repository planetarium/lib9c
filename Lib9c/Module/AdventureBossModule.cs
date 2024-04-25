using System.Globalization;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.AdventureBoss;

namespace Nekoyume.Module
{
    public static class BountyBoardModule
    {
        public static Address Derive(long season) =>
            Addresses.BountyBoard.Derive(season.ToString(CultureInfo.InvariantCulture));
        public static BountyBoard GetBountyBoard(this IWorldState worldState, long season)
        {
            IAccountState account = worldState.GetAccountState(Addresses.BountyBoard);
            if (account.GetState(Derive(season)) is { } a)
            {
                return new BountyBoard(a);
            }

            throw new FailedLoadStateException("");
        }

        public static IWorld SetBountyBoard(this IWorld world, long season, BountyBoard bountyBoard)
        {
            IAccount account = world.GetAccount(Addresses. BountyBoard);
            account = account.SetState(Derive(season), bountyBoard.Bencoded);
            return world.SetAccount(Addresses.BountyBoard, account);
        }
    }
}
