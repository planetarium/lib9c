namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;

    /// <summary>
    /// Test helper that injects an in-memory <see cref="CollectionSheet"/> row with very large
    /// stat add-modifiers and activates it on the target avatar.
    /// </summary>
    /// <remarks>
    /// Some action tests target stages whose production <c>SweepRequiredCPSheet</c> /
    /// <c>StageSheet</c> values require an extremely high player CP or battle stats. Rather than
    /// mutating those production CSVs, this helper grants the avatar a synthetic collection that
    /// raises ATK/DEF/HP enough to clear both the sweep CP check and the simulator battle.
    /// </remarks>
    public static class AvatarCpBooster
    {
        // The collection row id is chosen well outside the production range so it cannot
        // collide with real collection ids loaded from CSV.
        public const int CollectionId = 999_999;

        // ATK and DEF contribute 10.5 CP per point; HP contributes 0.7 CP per point.
        // 2,000,000,000 ATK + 2,000,000,000 DEF yields ~42B CP — comfortably above the
        // highest SweepRequiredCP value used in tests — and provides enough offensive /
        // defensive headroom for the StageSimulator to consistently clear high-tier
        // stages (e.g. stage 451) across multiple deterministic plays.
        public const int AtkAdd = 2_000_000_000;
        public const int DefAdd = 2_000_000_000;
        public const int HpAdd = 2_000_000_000;

        /// <summary>
        /// Adds a synthetic <see cref="CollectionSheet"/> row to the in-memory state and
        /// activates it on <paramref name="avatarAddress"/>.
        /// </summary>
        /// <param name="state">The world state to augment.</param>
        /// <param name="sheets">The raw CSV dictionary returned by <c>TableSheetsImporter</c>.</param>
        /// <param name="avatarAddress">The avatar that should own the boosted collection.</param>
        /// <returns>The updated world state.</returns>
        public static IWorld Apply(
            IWorld state,
            IReadOnlyDictionary<string, string> sheets,
            Address avatarAddress)
        {
            var lines = sheets[nameof(CollectionSheet)]
                .Split('\n')
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            var cols = new List<string> { CollectionId.ToString() };

            // CollectionSheet has 6 required-material slots × 4 columns (item_id, count, level,
            // skill). The row parser skips slots whose item_id is empty or zero, so leaving them
            // blank yields a row with no material requirements.
            for (var i = 0; i < 24; i++)
            {
                cols.Add(string.Empty);
            }

            // Three stat-modifier triples: (stat_type, modify_type, modify_value).
            cols.AddRange(new[] { "HP", "Add", HpAdd.ToString() });
            cols.AddRange(new[] { "ATK", "Add", AtkAdd.ToString() });
            cols.AddRange(new[] { "DEF", "Add", DefAdd.ToString() });

            lines.Add(string.Join(",", cols));

            state = state.SetLegacyState(
                Addresses.TableSheet.Derive(nameof(CollectionSheet)),
                string.Join("\n", lines).Serialize());

            var collectionState = new CollectionState();
            collectionState.Ids.Add(CollectionId);
            return state.SetCollectionState(avatarAddress, collectionState);
        }
    }
}
