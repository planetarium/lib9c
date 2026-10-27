using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    /// <summary>
    /// Keeps what each world-open id means fixed across table patches. The first patch that
    /// carries an id records its identity; a later patch that gives the id a different identity is
    /// rejected. An id may be dropped from the sheet (retired), but its record stays, so it can
    /// only come back with the same identity. To fix a wrong rule, drop it and add a new id.
    /// </summary>
    /// <remarks>
    /// Counter values and confirmations are keyed by id, so changing what an id means would carry
    /// them over to a different rule: a counter would keep counts of something else, and a new
    /// opening reusing a confirmed id would open without its conditions. The identity is:
    /// <list type="bullet">
    /// <item><see cref="GlobalCounterSheet"/>: every column (type, target range, minimum level).
    /// </item>
    /// <item><see cref="WorldOpenSheet"/>: <c>world_id</c> and <c>stage_end</c>. The block window
    /// and the prerequisite stay adjustable; the prerequisite follows the stage_end line, so it has
    /// to change when an opening is inserted below.</item>
    /// <item><see cref="WorldOpenConditionSheet"/>: per <c>open_id</c>, which counters are grouped
    /// together. Group numbers and row order do not matter; thresholds stay adjustable.</item>
    /// </list>
    /// </remarks>
    public static class WorldOpenIdentity
    {
        /// <summary>
        /// Rejects <paramref name="csv"/> if it changes the identity of an id recorded earlier, and
        /// records the identity of every id seen for the first time. Any other sheet is left alone.
        /// </summary>
        /// <param name="states">The world before the patched sheet is stored.</param>
        /// <param name="tableName">The patched sheet's name.</param>
        /// <param name="csv">The patched sheet, already validated by
        /// <see cref="TableSheetPatchValidation"/>.</param>
        /// <returns>The world with the new identities recorded, or <paramref name="states"/> itself
        /// when nothing new was seen.</returns>
        /// <exception cref="SheetRowValidateException">An id's identity differs from its record.
        /// </exception>
        public static IWorld CheckAndRecord(IWorld states, string tableName, string csv)
        {
            IEnumerable<(string Key, string Identity)> identities;
            switch (tableName)
            {
                case nameof(GlobalCounterSheet):
                    var counters = new GlobalCounterSheet();
                    counters.Set(csv);
                    identities = counters.OrderedList.Select(r => (
                        Key("counter", r.Id),
                        string.Join(
                            ",",
                            r.Type.ToString(),
                            Number(r.TargetBegin),
                            Number(r.TargetEnd),
                            Number(r.MinAvatarLevel))));
                    break;

                case nameof(WorldOpenSheet):
                    var opens = new WorldOpenSheet();
                    opens.Set(csv);
                    identities = opens.OrderedList.Select(r => (
                        Key("open", r.Id),
                        string.Join(",", Number(r.WorldId), Number(r.StageEnd))));
                    break;

                case nameof(WorldOpenConditionSheet):
                    var conditions = new WorldOpenConditionSheet();
                    conditions.Set(csv);
                    identities = conditions.OrderedList
                        .Select(r => r.OpenId)
                        .Distinct()
                        .Select(openId => (Key("conditions", openId), Groups(conditions, openId)));
                    break;

                default:
                    return states;
            }

            var account = states.GetAccount(Addresses.WorldOpen);
            var changed = false;
            foreach (var (key, identity) in identities)
            {
                var address = Addresses.WorldOpen.Derive(key);
                if (account.GetState(address) is Text recorded)
                {
                    if (recorded.Value != identity)
                    {
                        throw new SheetRowValidateException(
                            $"{tableName}: {key} was recorded as ({recorded.Value}) and cannot " +
                            $"become ({identity}); drop it and add a new id instead.");
                    }

                    continue;
                }

                account = account.SetState(address, (Text)identity);
                changed = true;
            }

            return changed ? states.SetAccount(Addresses.WorldOpen, account) : states;
        }

        // Each group as its sorted counter ids, groups sorted, so renumbering groups or reordering
        // rows keeps the identity.
        private static string Groups(WorldOpenConditionSheet conditions, int openId) =>
            string.Join(
                "|",
                conditions.GroupsOf(openId)
                    .Select(group => string.Join(
                        ",",
                        group.Select(c => c.CounterId).Distinct().OrderBy(id => id).Select(Number)))
                    .OrderBy(group => group, System.StringComparer.Ordinal));

        private static string Key(string kind, int id) => $"identity_{kind}_{Number(id)}";

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
