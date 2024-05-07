using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.AdventureBoss;

namespace Nekoyume.Module
{
    // Use `Addresses.AdventureBoss` for AccountState to store adventure boss data itself.
    // ** Special: Use `0x0000000000000000000000000000000000000000` to get latest state
    // `Use 0x0000000000000000000000000000000000000001`, `0x2`, `0x3`, ... for season info. This is called SeasonAddress

    // Use `Addresses.BountyBoard` for AccountState to store all investment data
    // Use `0x0000000000000000000000000000000000000001`, `0x2`, `0x3`, ... for season bounty board.
    // Each BountyBoard has investors' data.

    // Use `Addresses.AdventureBossExplore` for AccountState to store all adventurer's data
    // Use `Address.Derive(AvatarAddress, SeasonAddress)` for individual avatar's explore info.
    public static class AdventureBossModule
    {
        public static readonly Address LatestSeasonAddress = new($"{0:X40}");

        private static string GetSeasonAsAddressForm(long season)
        {
            return $"{season:X40}";
        }

        /// <summary>
        /// Get brief adventure boss season info of latest season.
        /// This only has brief, readonly data, so we must use <see cref="SeasonInfo"/> to handle season itself.
        /// </summary>
        /// <returns>LatestSeason state. If no season is started, return a state with default value(0)</returns>
        public static LatestSeason GetLatestAdventureBossSeason(this IWorldState worldState)
        {
            var account = worldState.GetAccountState(Addresses.AdventureBoss);
            var latestSeason = account.GetState(LatestSeasonAddress);
            if (latestSeason is null)
            {
                return new LatestSeason(season: 0, startBlockIndex: 0, endBlockIndex: 0,
                    nextStartBlockIndex: 0);
            }

            return new LatestSeason(latestSeason);
        }

        public static IWorld SetLatestAdventureBossSeason(this IWorld world,
            SeasonInfo latestSeasonInfo)
        {
            var account = world.GetAccount(Addresses.AdventureBoss);
            var latestSeason = new LatestSeason(
                season: latestSeasonInfo.Season,
                startBlockIndex: latestSeasonInfo.StartBlockIndex,
                endBlockIndex: latestSeasonInfo.EndBlockIndex,
                nextStartBlockIndex: latestSeasonInfo.NextStartBlockIndex
            );
            account = account.SetState(LatestSeasonAddress, latestSeason.Bencoded);
            return world.SetAccount(Addresses.AdventureBoss, account);
        }

        public static SeasonInfo GetSeasonInfo(this IWorldState worldState, long season)
        {
            var account = worldState.GetAccountState(Addresses.AdventureBoss);
            if (account.GetState(Addresses.AdventureBoss) is { } a)
            {
                return new SeasonInfo(a);
            }

            throw new FailedLoadStateException("");
        }

        public static IWorld SetSeasonInfo(this IWorld world, SeasonInfo seasonInfo)
        {
            var seasonAddress = new Address(GetSeasonAsAddressForm(seasonInfo.Season));
            var account = world.GetAccount(Addresses.AdventureBoss);
            account = account.SetState(seasonAddress, seasonInfo.Bencoded);
            return world.SetAccount(Addresses.AdventureBoss, account);
        }

        public static BountyBoard GetBountyBoard(this IWorldState worldState, long season)
        {
            var account = worldState.GetAccountState(Addresses.BountyBoard);
            if (account.GetState(new Address(GetSeasonAsAddressForm(season))) is { } a)
            {
                return new BountyBoard(a);
            }

            throw new FailedLoadStateException("");
        }

        public static IWorld SetBountyBoard(this IWorld world, long season, BountyBoard bountyBoard)
        {
            var account = world.GetAccount(Addresses.BountyBoard);
            account = account.SetState(
                new Address(GetSeasonAsAddressForm(season)),
                bountyBoard.Bencoded
            );
            return world.SetAccount(Addresses.BountyBoard, account);
        }

        public static ExploreInfo GetExploreInfo(this IWorldState worldState, long season,
            Address avatarAddress)
        {
            var account = worldState.GetAccountState(Addresses.AdventureBossExplore);
            if (account.GetState(avatarAddress.Derive(GetSeasonAsAddressForm(season))) is { } a)
            {
                return new ExploreInfo(a);
            }

            throw new FailedLoadStateException("");
        }

        public static IWorld SetExploreInfo(this IWorld world,
            long season, ExploreInfo exploreInfo)
        {
            var account = world.GetAccount(Addresses.AdventureBossExplore);
            account = account.SetState(
                exploreInfo.AvatarAddress.Derive(GetSeasonAsAddressForm(season)),
                exploreInfo.Bencoded
            );
            return world.SetAccount(Addresses.AdventureBossExplore, account);
        }
    }
}
