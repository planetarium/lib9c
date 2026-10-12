#nullable enable
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    /// <summary>
    /// A <see cref="BoostScheduleSheet"/> already checked to apply to the enhancement cost of one
    /// rune, <see cref="RuneId"/>: an enhancement cost row was active and the rune was summonable
    /// when it was resolved. Only <see cref="RuneHelper.ResolveEnhancementCostBoost"/> creates
    /// one, and <see cref="RuneHelper.TryEnhancement"/> and
    /// <see cref="RuneHelper.ApplyEnhancementCostBoost"/> ignore it for any other rune, so a
    /// boost only ever discounts the rune it was resolved for — neither handing over the raw
    /// patched sheet nor reusing a boost after switching runes can skip the check.
    /// </summary>
    public sealed class RuneEnhancementCostBoost
    {
        internal RuneEnhancementCostBoost(BoostScheduleSheet boostScheduleSheet, int runeId)
        {
            BoostScheduleSheet = boostScheduleSheet;
            RuneId = runeId;
        }

        /// <summary>
        /// The rune this boost was resolved for, and the only one it adjusts.
        /// </summary>
        public int RuneId { get; }

        internal BoostScheduleSheet BoostScheduleSheet { get; }
    }
}
