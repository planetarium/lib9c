using System;
using Nekoyume.Helper;

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

        /// <summary>
        /// calculate defense.
        /// </summary>
        /// <param name="targetDefense">enemy <see cref="Model.Stat.StatType.DEF"/> stats</param>
        /// <param name="armorPenetration">caster <see cref="Model.Stat.StatType.ArmorPenetration"/> stats</param>
        /// <returns>calculated final defense.</returns>
        public static long GetFinalDefense(long targetDefense, long armorPenetration)
        {
            return Math.Clamp(targetDefense - armorPenetration, 0, long.MaxValue);
        }

        /// <summary>
        /// calculate reduced damage. minimum 1.
        /// </summary>
        /// <param name="damage">damage</param>
        /// <param name="drv">enemy <see cref="Model.Stat.StatType.DRV"/> stats</param>
        /// <param name="drr">enemy <see cref="Model.Stat.StatType.DRR"/> stats</param>
        /// <returns>calculated damage.</returns>
        public static long GetReducedDamage(long damage, long drv, long drr)
        {
            return Math.Max(1, NumberConversionHelper.SafeDecimalToInt64((damage - drv) * GetDamageReductionRate(drr)));
        }
    }
}
