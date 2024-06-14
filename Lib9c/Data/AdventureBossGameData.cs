using System.Collections.Generic;
using Libplanet.Types.Assets;

namespace Nekoyume.Data
{
    public static class AdventureBossGameData
    {
        public const decimal NcgRuneRatio = 2.5m;


        public struct ClaimableReward
        {
            public FungibleAssetValue? NcgReward;
            public Dictionary<int, int> ItemReward;
            public Dictionary<int, int> FavReward;
        }
    }
}
