using Nekoyume.TableData;

namespace Nekoyume.Action
{
    /// <summary>
    /// The per-sheet checks <see cref="PatchTableSheet"/> and
    /// <see cref="PatchTableSheetCompressed"/> run before a CSV lands on the chain. Both actions
    /// call this one helper so the two paths can never disagree about what is accepted.
    /// </summary>
    public static class TableSheetPatchValidation
    {
        /// <summary>
        /// Validates <paramref name="csv"/> for the sheet <paramref name="tableName"/>. Sheets
        /// without a validator are accepted as they are.
        /// </summary>
        /// <remarks>
        /// The validated sheets are read leniently at runtime: a row that does not parse is kept
        /// but never matches, so a malformed patch would otherwise land and silently mis-configure
        /// the game with nothing reporting it. The world-open sheets are also read by every
        /// HackAndSlash, where an empty CSV would make <see cref="Sheet{TKey,TValue}.Set"/> throw
        /// and fail all of them, so an empty or whitespace-only CSV is rejected for them too.
        /// </remarks>
        /// <exception cref="SheetRowValidateException">The CSV is malformed.</exception>
        public static void Validate(string tableName, string csv)
        {
            switch (tableName)
            {
                // A row no lookup can match reads as "unrestricted", and nothing reports that it
                // went missing.
                case nameof(RestrictionSheet):
                    RestrictionSheet.ValidateCsv(csv);
                    break;

                // A row that does not parse is read as no adjustment, so it would otherwise land
                // and silently never apply.
                case nameof(BoostScheduleSheet):
                    BoostScheduleSheet.ValidateCsv(csv);
                    break;

                case nameof(GlobalCounterSheet):
                    RejectEmpty(tableName, csv);
                    GlobalCounterSheet.ValidateCsv(csv);
                    break;

                case nameof(WorldOpenSheet):
                    RejectEmpty(tableName, csv);
                    WorldOpenSheet.ValidateCsv(csv);
                    break;

                case nameof(WorldOpenConditionSheet):
                    RejectEmpty(tableName, csv);
                    WorldOpenConditionSheet.ValidateCsv(csv);
                    break;
            }
        }

        private static void RejectEmpty(string tableName, string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
            {
                throw new SheetRowValidateException(
                    $"{tableName}: the CSV must not be empty; it needs at least the header line.");
            }
        }
    }
}
