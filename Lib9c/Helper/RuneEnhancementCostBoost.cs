#nullable enable
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    /// <summary>
    /// A <see cref="BoostScheduleSheet"/> already checked to apply to one rune's enhancement cost:
    /// an enhancement cost row is active and the rune is summonable. Only
    /// <see cref="RuneHelper.ResolveEnhancementCostBoost"/> creates one, so a cost can never be
    /// adjusted by handing the raw patched sheet to <see cref="RuneHelper.TryEnhancement"/> or
    /// <see cref="RuneHelper.ApplyEnhancementCostBoost"/> and skipping that check.
    /// </summary>
    public sealed class RuneEnhancementCostBoost
    {
        internal RuneEnhancementCostBoost(BoostScheduleSheet boostScheduleSheet)
        {
            BoostScheduleSheet = boostScheduleSheet;
        }

        internal BoostScheduleSheet BoostScheduleSheet { get; }
    }
}
