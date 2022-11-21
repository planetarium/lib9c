using System;
using Libplanet;

namespace Nekoyume.Action.Factory
{
    public static class RuneEnhancementFactory
    {
        public static long GetAvailableBlockIndex(int version)
        {
            switch (version)
            {
                case 1:
                    return RuneEnhancement01.AvailableBlockIndex;
                default:
                    return Int32.MaxValue;
            }
        }

        public static int GetAvailableVersion(long blockIndex)
        {
            if (blockIndex >= RuneEnhancement01.AvailableBlockIndex)
            {
                return RuneEnhancement01.Version;
            }

            return -1;
        }

        public static GameAction RuneEnhancement(
            long blockIndex,
            Address avatarAddress,
            int runeId,
            int tryCount
        )
        {
            if (blockIndex >= RuneEnhancement01.AvailableBlockIndex)
            {
                return new RuneEnhancement01()
                {
                    AvatarAddress = avatarAddress,
                    RuneId = runeId,
                    TryCount = tryCount
                };
            }

            return null;
        }

        public static GameAction RuneEnhancement(
            int version,
            Address avatarAddress,
            int runeId,
            int tryCount
        )
        {
            switch (version)
            {
                case 1:
                    return new RuneEnhancement01()
                    {
                        AvatarAddress = avatarAddress,
                        RuneId = runeId,
                        TryCount = tryCount
                    };
                default:
                    return null;
            }
        }
    }
}
