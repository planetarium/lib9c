namespace Lib9c.Tests.Action
{
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;

    /// <summary>
    /// Shared setup for tests of <see cref="BoostScheduleSheet.Targets.StageItemReward"/> and
    /// <see cref="BoostScheduleSheet.Targets.StageFavReward"/>.
    /// </summary>
    public static class StageRewardBoostFixture
    {
        public const string FavTicker = "CRYSTAL";

        public const long BoostStartBlock = 100;

        public const long BoostEndBlock = 200;

        /// <summary>
        /// A schedule with no row at all.
        /// </summary>
        public const string EmptyCsv = "id,target,target_id,op,value,start_block,end_block\n";

        public static readonly Address BoostScheduleSheetAddress =
            Addresses.GetSheetAddress<BoostScheduleSheet>();

        /// <summary>
        /// A schedule doubling both the items and the fungible assets of <paramref name="stageId"/>
        /// in [<see cref="BoostStartBlock"/>, <see cref="BoostEndBlock"/>), plus rows that must
        /// not apply: another target, and a stage range that excludes the stage.
        /// </summary>
        public static string BoostCsv(int stageId) =>
            "id,target,target_id,op,value,start_block,end_block,_memo\n" +
            $"1,{BoostScheduleSheet.Targets.EquipmentSummonGuarantee},*,ADD,5,{BoostStartBlock},{BoostEndBlock},other target\n" +
            $"2,{BoostScheduleSheet.Targets.StageItemReward},{stageId + 1}~{stageId + 10},MUL,3,{BoostStartBlock},{BoostEndBlock},other stages\n" +
            $"3,{BoostScheduleSheet.Targets.StageItemReward},1~{stageId},MUL,2,{BoostStartBlock},{BoostEndBlock},\n" +
            $"4,{BoostScheduleSheet.Targets.StageFavReward},{stageId},MUL,2,{BoostStartBlock},{BoostEndBlock},\n";

        /// <summary>
        /// A schedule whose rows are all active in [<see cref="BoostStartBlock"/>,
        /// <see cref="BoostEndBlock"/>) but none of which covers <paramref name="stageId"/>.
        /// </summary>
        /// <summary>
        /// A schedule applying <paramref name="op"/> <paramref name="value"/> to both the items
        /// and the fungible assets of <paramref name="stageId"/> in
        /// [<see cref="BoostStartBlock"/>, <see cref="BoostEndBlock"/>).
        /// </summary>
        public static string BoostCsv(int stageId, string op, string value) =>
            "id,target,target_id,op,value,start_block,end_block\n" +
            $"1,{BoostScheduleSheet.Targets.StageItemReward},{stageId},{op},{value},{BoostStartBlock},{BoostEndBlock}\n" +
            $"2,{BoostScheduleSheet.Targets.StageFavReward},{stageId},{op},{value},{BoostStartBlock},{BoostEndBlock}\n";

        public static string UnmatchedCsv(int stageId) =>
            "id,target,target_id,op,value,start_block,end_block\n" +
            $"1,{BoostScheduleSheet.Targets.EquipmentSummonGuarantee},*,ADD,5,{BoostStartBlock},{BoostEndBlock}\n" +
            $"2,{BoostScheduleSheet.Targets.StageItemReward},{stageId + 1}~{stageId + 10},MUL,3,{BoostStartBlock},{BoostEndBlock}\n" +
            $"3,{BoostScheduleSheet.Targets.StageFavReward},1~{stageId - 1},MUL,3,{BoostStartBlock},{BoostEndBlock}\n";

        public static IWorld WithBoostSchedule(IWorld state, string csv) =>
            state.SetLegacyState(BoostScheduleSheetAddress, csv.Serialize());

        public static IWorld WithoutBoostSchedule(IWorld state) =>
            state.SetLegacyState(BoostScheduleSheetAddress, Null.Value);

        /// <summary>
        /// Rewrites <paramref name="stageId"/> of a <c>StageSheet</c> CSV so that every clear
        /// draws exactly <paramref name="amount"/> of <see cref="FavTicker"/>.
        /// </summary>
        public static string WithFavReward(string stageCsv, int stageId, int amount)
        {
            var lines = stageCsv.Split('\n').ToList();
            var header = lines[0].TrimEnd('\r').Split(',').ToList();
            for (var i = 1; i < lines.Count; i++)
            {
                var cols = lines[i].TrimEnd('\r').Split(',').ToList();
                if (cols[0] != stageId.ToString())
                {
                    continue;
                }

                while (cols.Count < header.Count)
                {
                    cols.Add(string.Empty);
                }

                for (var fav = 1; fav <= 5; fav++)
                {
                    var used = fav == 1;
                    cols[header.IndexOf($"fungible_asset_reward_ticker_{fav}")] = used ? FavTicker : string.Empty;
                    cols[header.IndexOf($"fungible_asset_reward_ratio_{fav}")] = used ? "100" : string.Empty;
                    cols[header.IndexOf($"fungible_asset_reward_min_{fav}")] = used ? amount.ToString() : string.Empty;
                    cols[header.IndexOf($"fungible_asset_reward_max_{fav}")] = used ? amount.ToString() : string.Empty;
                }

                cols[header.IndexOf("fav_drop_min")] = "1";
                cols[header.IndexOf("fav_drop_max")] = "1";
                lines[i] = string.Join(",", cols);
                return string.Join('\n', lines);
            }

            throw new System.ArgumentException($"stage {stageId} not found", nameof(stageId));
        }

        /// <summary>
        /// Rewrites <paramref name="stageId"/> of a <c>StageSheet</c> CSV so that its first drop
        /// is <paramref name="itemId"/> (1 to 2 per draw) and every clear draws
        /// <paramref name="dropMin"/> to <paramref name="dropMax"/> items.
        /// </summary>
        public static string WithItemReward(string stageCsv, int stageId, int itemId, int dropMin, int dropMax)
        {
            var lines = stageCsv.Split('\n').ToList();
            var header = lines[0].TrimEnd('\r').Split(',').ToList();
            for (var i = 1; i < lines.Count; i++)
            {
                var cols = lines[i].TrimEnd('\r').Split(',').ToList();
                if (cols[0] != stageId.ToString())
                {
                    continue;
                }

                cols[header.IndexOf("item1")] = itemId.ToString();
                cols[header.IndexOf("item1_ratio")] = "0.3";
                cols[header.IndexOf("item1_min")] = "1";
                cols[header.IndexOf("item1_max")] = "2";
                cols[header.IndexOf("min_drop")] = dropMin.ToString();
                cols[header.IndexOf("max_drop")] = dropMax.ToString();
                lines[i] = string.Join(",", cols);
                return string.Join('\n', lines);
            }

            throw new System.ArgumentException($"stage {stageId} not found", nameof(stageId));
        }

        public static BoostScheduleSheet.Row Row(string target, string op, decimal value)
        {
            var sheet = new BoostScheduleSheet();
            sheet.Set(
                "id,target,target_id,op,value,start_block,end_block\n" +
                $"1,{target},*,{op},{value.ToString(CultureInfo.InvariantCulture)},0,1\n");
            return sheet[1];
        }
    }
}
