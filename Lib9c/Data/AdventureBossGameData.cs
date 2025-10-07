using System.Collections.Generic;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Data
{
    public static class AdventureBossGameData
    {
        public static readonly Address AdventureBossOperationalAddress =
            new ("0x6923bb64139d23C677D858CcA7e1e2a31457bB8D");

        public struct ClaimableReward
        {
            public FungibleAssetValue? NcgReward;
            public Dictionary<int, int> ItemReward;
            public Dictionary<int, int> FavReward;

            public bool IsEmpty()
            {
                return (NcgReward is null || ((FungibleAssetValue)NcgReward).RawValue <= 0) &&
                       ItemReward.Count == 0 && FavReward.Count == 0;
            }
        }
    }
}
