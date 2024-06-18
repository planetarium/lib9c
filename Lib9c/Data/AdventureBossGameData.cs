using System.Collections.Generic;
using Libplanet.Types.Assets;

namespace Nekoyume.Data
{
    public static class AdventureBossGameData
    {
        public struct ClaimableReward
        {
            public FungibleAssetValue? NcgReward;
            public Dictionary<int, int> ItemReward;
            public Dictionary<int, int> FavReward;
        }
    }
}
