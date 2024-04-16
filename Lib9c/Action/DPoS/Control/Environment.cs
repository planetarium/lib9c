using System;
using System.Numerics;

namespace Nekoyume.Action.DPoS.Control
{
    public static class Environment
    {
        public const long SignedBlocksWindow = 10000;

        public const double MinSignedPerWindow = 0.5;

        public static readonly TimeSpan DowntimeJailDuration = TimeSpan.FromSeconds(60);

        public static readonly BigInteger SlashFractionDoubleSign = new BigInteger(20); // 0.05

        public static readonly BigInteger SlashFractionDowntime = new BigInteger(10000); // 0.0001

        public const long ValidatorUpdateDelay = 1;

        public const long MaxAgeNumBlocks = 100000;

        public static readonly TimeSpan MaxAgeDuration = TimeSpan.FromSeconds(172800); // s (즉, 48시간)

        public const long MaxBytes = 1048576; // (즉, 1MB)

        public static readonly DateTimeOffset DoubleSignJailEndTime = DateTimeOffset.FromUnixTimeSeconds(253402300799);

        public const int MissedBlockBitmapChunkSize = 1024;
    }
}
