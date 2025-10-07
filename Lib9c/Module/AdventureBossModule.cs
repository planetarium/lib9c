using Bencodex.Types;
using Lib9c.Action;
using Lib9c.Helper;
using Lib9c.Model.AdventureBoss;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.Module
{
    public static class AdventureBossModule
    {
        public static readonly Address LatestSeasonAddress = new ($"{0:X40}");


        /// <summary>
        /// Get brief adventure boss season info of latest season.
        /// This only has brief, readonly data, so we must use <see cref="SeasonInfo"/> to handle season itself.
        /// </summary>
        /// <returns>LatestSeason state. If no season is started, return a state with default value(0)</returns>
        public static SeasonInfo GetLatestAdventureBossSeason(this IWorldState worldState)
        {
            var account = worldState.GetAccountState(Addresses.AdventureBoss);
            var latestSeason = account.GetState(LatestSeasonAddress);
            return latestSeason is null
                ? new SeasonInfo(season: 0, 0, 0, 0)
                : new SeasonInfo((List)latestSeason);
        }

        public static IWorld SetLatestAdventureBossSeason(this IWorld world,
            SeasonInfo latestSeasonInfo)
        {
            var account = world.GetAccount(Addresses.AdventureBoss);
            account = account.SetState(LatestSeasonAddress, latestSeasonInfo.Bencoded);
            return world.SetAccount(Addresses.AdventureBoss, account);
        }

        // Use `Addresses.AdventureBoss` for AccountState to store adventure boss data itself.
        // ** Special: Use `0x0000000000000000000000000000000000000000` to get latest state
        // `Use 0x0000000000000000000000000000000000000001`, `0x2`, `0x3`, ... for season info. This is called SeasonAddress
        public static SeasonInfo GetSeasonInfo(this IWorldState worldState, long season)
        {
            var account = worldState.GetAccountState(Addresses.AdventureBoss);
            if (account.GetState(new Address(AdventureBossHelper.GetSeasonAsAddressForm(season))) is { } a)
            {
                return new SeasonInfo((List)a);
            }

            throw new FailedLoadStateException("");
        }

        public static IWorld SetSeasonInfo(this IWorld world, SeasonInfo seasonInfo)
        {
            var seasonAddress = new Address(AdventureBossHelper.GetSeasonAsAddressForm(seasonInfo.Season));
            var account = world.GetAccount(Addresses.AdventureBoss);
            account = account.SetState(seasonAddress, seasonInfo.Bencoded);
            return world.SetAccount(Addresses.AdventureBoss, account);
        }

        // Use `Addresses.BountyBoard` for AccountState to store all investment data
        // Use `0x0000000000000000000000000000000000000001`, `0x2`, `0x3`, ... for season bounty board.
        // Each BountyBoard has investors' data.
        public static BountyBoard GetBountyBoard(this IWorldState worldState, long season)
        {
            var account = worldState.GetAccountState(Addresses.BountyBoard);
            if (account.GetState(new Address(AdventureBossHelper.GetSeasonAsAddressForm(season))) is { } a)
            {
                return new BountyBoard((List)a);
            }

            return new BountyBoard(season);
        }

        public static IWorld SetBountyBoard(this IWorld world, long season, BountyBoard bountyBoard)
        {
            var account = world.GetAccount(Addresses.BountyBoard);
            account = account.SetState(
                new Address(AdventureBossHelper.GetSeasonAsAddressForm(season)),
                bountyBoard.Bencoded
            );
            return world.SetAccount(Addresses.BountyBoard, account);
        }

        public static bool TryGetExploreBoard(this IWorldState worldState, long season,
            out ExploreBoard exploreBoard)
        {
            try
            {
                exploreBoard = GetExploreBoard(worldState, season);
                return true;
            }
            catch (FailedLoadStateException)
            {
                exploreBoard = null;
                return false;
            }
        }

        public static ExploreBoard GetExploreBoard(this IWorldState worldState, long season)
        {
            var account = worldState.GetAccountState(Addresses.ExploreBoard);
            if (account.GetState(new Address(AdventureBossHelper.GetSeasonAsAddressForm(season))) is { } a)
            {
                return new ExploreBoard((List)a);
            }

            throw new FailedLoadStateException("");
        }

        public static IWorld SetExploreBoard(this IWorld world, long season,
            ExploreBoard exploreBoard)
        {
            var account = world.GetAccount(Addresses.ExploreBoard);
            account = account.SetState(
                new Address(AdventureBossHelper.GetSeasonAsAddressForm(season)),
                exploreBoard.Bencoded()
            );
            return world.SetAccount(Addresses.ExploreBoard, account);
        }

        public static ExplorerList GetExplorerList(this IWorldState worldState, long season)
        {
            var account = worldState.GetAccountState(Addresses.ExplorerList);
            if (account.GetState(new Address(AdventureBossHelper.GetSeasonAsAddressForm(season))) is { } a)
            {
                return new ExplorerList((List)a);
            }

            throw new FailedLoadStateException("");
        }

        public static IWorld SetExplorerList(this IWorld world, long season,
            ExplorerList explorerList)
        {
            var account = world.GetAccount(Addresses.ExplorerList);
            account = account.SetState(
                new Address(AdventureBossHelper.GetSeasonAsAddressForm(season)),
                explorerList.Bencoded
            );
            return world.SetAccount(Addresses.ExplorerList, account);

        }

        // Use `Addresses.AdventureBossExplore` for AccountState to store all adventurer's data
        // Use `Address.Derive(AvatarAddress, SeasonAddress)` for individual avatar's explore info.
        public static bool TryGetExplorer(this IWorldState worldState, long season,
            Address avatarAddress, out Explorer explorer)
        {
            explorer = null;
            try
            {
                explorer = GetExplorer(worldState, season, avatarAddress);
                return true;
            }
            catch (FailedLoadStateException)
            {
                return false;
            }
        }

        public static Explorer GetExplorer(this IWorldState worldState, long season,
            Address avatarAddress)
        {
            var account = worldState.GetAccountState(Addresses.ExploreBoard);
            if (account.GetState(avatarAddress.Derive(AdventureBossHelper.GetSeasonAsAddressForm(season))) is { } a)
            {
                return new Explorer(a);
            }

            throw new FailedLoadStateException("");
        }

        public static IWorld SetExplorer(this IWorld world, long season, Explorer explorer)
        {
            var account = world.GetAccount(Addresses.ExploreBoard);
            account = account.SetState(
                explorer.AvatarAddress.Derive(AdventureBossHelper.GetSeasonAsAddressForm(season)),
                explorer.Bencoded
            );
            return world.SetAccount(Addresses.ExploreBoard, account);
        }
    }
}
