using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.AdventureBoss;

namespace Nekoyume.Module
{
    public static class AdventureBossModule
    {
        public static BountyBoard GetBountyBoard(this IWorldState worldState, long season)
        {
            var seasonAddress = Addresses.AdventureSeasonAddress(season);
            IAccountState account = worldState.GetAccountState(seasonAddress);
            if (account.GetState(Addresses.BountyBoard) is { } a)
            {
                return new BountyBoard(a);
            }

            throw new FailedLoadStateException("");
        }

        public static IWorld SetBountyBoard(this IWorld world, long season, BountyBoard bountyBoard)
        {
            var seasonAddress = Addresses.AdventureSeasonAddress(season);
            IAccount account = world.GetAccount(seasonAddress);
            account = account.SetState(Addresses.BountyBoard, bountyBoard.Bencoded);
            return world.SetAccount(seasonAddress, account);
        }

        public static AdventureInfo GetAdventureInfo(this IWorldState worldState, long season, Address avatarAddress)
        {
            var seasonAddress = Addresses.AdventureSeasonAddress(season);
            IAccountState account = worldState.GetAccountState(seasonAddress);
            if (account.GetState(avatarAddress) is { } a)
            {
                return new AdventureInfo(a);
            }

            throw new FailedLoadStateException("");
        }

        public static IWorld SetAdventureInfo(this IWorld world, long season, AdventureInfo adventureInfo)
        {
            var seasonAddress = Addresses.AdventureSeasonAddress(season);
            IAccount account = world.GetAccount(seasonAddress);
            account = account.SetState(adventureInfo.AvatarAddress, adventureInfo.Bencoded);
            return world.SetAccount(seasonAddress, account);
        }
    }
}
