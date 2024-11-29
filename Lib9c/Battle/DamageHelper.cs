namespace Nekoyume.Battle
{
    public static class DamageHelper
    {
        /// <summary>
        /// calculate damage reduction rate.
        /// </summary>
        /// <param name="drr">enemy <see cref="Model.Stat.StatType.DRR"/> stats</param>
        /// <returns>damage reduction rate.</returns>
        public static decimal GetDamageReductionRate(long drr)
        {
            switch (drr)
            {
                case > 8100L:
                    drr = 8100L;
                    break;
                case < 0L:
                    return 0m;
            }

            return 1 - drr / 10000m;
        }
    }
}
