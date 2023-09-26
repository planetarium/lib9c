using System;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Statics
{
    public static class RapidCombination
    {
        public static int CalculateHourglassCountV0(GameConfigState state, long diff)
        {
            return CalculateHourglassCountV0(state.HourglassPerBlock, diff);
        }

        public static int CalculateHourglassCountV0(decimal hourglassPerBlock, long diff)
        {
            if (diff <= 0)
            {
                return 0;
            }

            var cost = Math.Ceiling(diff / hourglassPerBlock);
            return Math.Max(1, (int)cost);
        }
    }
}
