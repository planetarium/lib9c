using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class StageSheet : Sheet<int, StageSheet.Row>
    {
        [Serializable]
        public class RewardData
        {
            public int ItemId { get; }
            public decimal Ratio { get; }
            public int Min { get; }
            public int Max { get; }

            public RewardData(int itemId, decimal ratio, int min, int max)
            {
                ItemId = itemId;
                Ratio = ratio;
                Min = min;
                Max = max;
            }
        }

        [Serializable]
        public class FavRewardData
        {
            /// <summary>
            /// Gets the currency ticker of the fungible asset reward (e.g., <c>CRYSTAL</c>).
            /// </summary>
            public string Ticker { get; }

            /// <summary>
            /// Gets the weight used for random selection.
            /// </summary>
            public decimal Ratio { get; }

            /// <summary>
            /// Gets the minimum amount granted when selected.
            /// </summary>
            public int Min { get; }

            /// <summary>
            /// Gets the maximum amount granted when selected.
            /// </summary>
            public int Max { get; }

            /// <summary>
            /// Initializes a new instance of <see cref="FavRewardData"/>.
            /// </summary>
            /// <param name="ticker">The fungible asset ticker.</param>
            /// <param name="ratio">The selection weight.</param>
            /// <param name="min">Minimum granted amount (inclusive).</param>
            /// <param name="max">Maximum granted amount (inclusive).</param>
            public FavRewardData(string ticker, decimal ratio, int min, int max)
            {
                Ticker = ticker;
                Ratio = ratio;
                Min = min;
                Max = max;
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            // FIXME AudioController.MusicCode.StageGreen과 중복
            private const string DefaultBGM = "bgm_stage_green";
            
            public override int Key => Id;
            public int Id { get; private set; }
            public int CostAP { get; private set; }
            /// <summary>
            /// Gets the additional entry cost material item ID required to play this stage.
            /// A value of 0 means no additional entry material cost is required.
            /// </summary>
            public int EntryCostItemId { get; private set; }

            /// <summary>
            /// Gets the additional entry cost material item count required per play for this stage.
            /// A value of 0 means no additional entry material cost is required.
            /// </summary>
            public int EntryCostItemCount { get; private set; }
            public int TurnLimit { get; private set; }
            public List<StatModifier> EnemyInitialStatModifiers { get; private set; }
            public string Background { get; private set; }
            public string BGM { get; private set; }
            public List<RewardData> Rewards { get; private set; }

            public int DropItemMin { get; private set; }
            public int DropItemMax { get; private set; }

            /// <summary>
            /// Gets the pool of fungible asset reward candidates for this stage.
            /// Rewards are selected by weight (<see cref="FavRewardData.Ratio"/>) and granted
            /// as (ticker, amount) pairs.
            /// </summary>
            public List<FavRewardData> FavRewards { get; private set; }

            /// <summary>
            /// Gets the minimum number of draws from <see cref="FavRewards"/> (inclusive).
            /// </summary>
            public int FavDropMin { get; private set; }

            /// <summary>
            /// Gets the maximum number of draws from <see cref="FavRewards"/> (inclusive).
            /// </summary>
            public int FavDropMax { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = TryParseInt(fields[0], out var id) ? id : 0;
                CostAP = TryParseInt(fields[1], out var costAP) ? costAP : 0;
                TurnLimit = TryParseInt(fields[2], out var turnLimit) ? turnLimit : 0;
                EnemyInitialStatModifiers = new List<StatModifier>();
                for (var i = 0; i < 6; i++)
                {
                    if (!TryParseInt(fields[3 + i], out var option) ||
                        option == 0)
                        continue;

                    switch (i)
                    {
                        case 0:
                            EnemyInitialStatModifiers.Add(new StatModifier(StatType.HP, StatModifier.OperationType.Percentage, option));
                            break;
                        case 1:
                            EnemyInitialStatModifiers.Add(new StatModifier(StatType.ATK, StatModifier.OperationType.Percentage, option));
                            break;
                        case 2:
                            EnemyInitialStatModifiers.Add(new StatModifier(StatType.DEF, StatModifier.OperationType.Percentage, option));
                            break;
                        case 3:
                            EnemyInitialStatModifiers.Add(new StatModifier(StatType.CRI, StatModifier.OperationType.Percentage, option));
                            break;
                        case 4:
                            EnemyInitialStatModifiers.Add(new StatModifier(StatType.HIT, StatModifier.OperationType.Percentage, option));
                            break;
                        case 5:
                            EnemyInitialStatModifiers.Add(new StatModifier(StatType.SPD, StatModifier.OperationType.Percentage, option));
                            break;
                    }
                    
                }
                
                Background = fields[9];
                BGM = string.IsNullOrEmpty(fields[10])
                    ? DefaultBGM
                    : fields[10];
                Rewards = new List<RewardData>();
                for (var i = 0; i < 10; i++)
                {
                    var offset = i * 4;
                    if (!TryParseInt(fields[11 + offset], out var itemId))
                        continue;
                    
                    Rewards.Add(new RewardData(
                        itemId,
                        TryParseDecimal(fields[12 + offset], out var ratio) ? ratio : 0m,
                        TryParseInt(fields[13 + offset], out var min) ? min : 0,
                        TryParseInt(fields[14 + offset], out var max) ? max : 0
                    ));
                }

                DropItemMin = TryParseInt(fields[51], out var dropMin) ? dropMin : 0;
                DropItemMax = TryParseInt(fields[52], out var dropMax) ? dropMax : 0;

                // Optional FAV reward columns (fields 53-72):
                // fungible_asset_reward_ticker_N, ratio_N, min_N, max_N (4 fields × 5 entries)
                FavRewards = new List<FavRewardData>();
                for (var i = 0; i < 5; i++)
                {
                    var favBase = 53 + i * 4;
                    if (fields.Count <= favBase || string.IsNullOrEmpty(fields[favBase]))
                        break;
                    if (!TryParseDecimal(fields[favBase + 1], out var favRatio))
                        break;
                    var favMin = fields.Count > favBase + 2 && TryParseInt(fields[favBase + 2], out var mn) ? mn : 0;
                    var favMax = fields.Count > favBase + 3 && TryParseInt(fields[favBase + 3], out var mx) ? mx : favMin;
                    FavRewards.Add(new FavRewardData(fields[favBase], favRatio, favMin, favMax));
                }

                // Optional FAV drop count columns (fields 73-74):
                // fav_drop_min, fav_drop_max — how many times to draw from the FAV pool.
                FavDropMin = fields.Count > 73 && TryParseInt(fields[73], out var favDropMin) ? favDropMin : 1;
                FavDropMax = fields.Count > 74 && TryParseInt(fields[74], out var favDropMax) ? favDropMax : FavDropMin;

                // Optional entry cost columns (fields 75-76), appended after FAV drop count:
                // - entry_cost_item_id
                // - entry_cost_item_count
                EntryCostItemId = fields.Count > 75 && TryParseInt(fields[75], out var entryCostItemId)
                    ? entryCostItemId
                    : 0;
                EntryCostItemCount = fields.Count > 76 && TryParseInt(fields[76], out var entryCostItemCount)
                    ? entryCostItemCount
                    : 0;
            }

            /// <summary>
            /// Creates a shallow clone of this row with a new stage ID.
            /// </summary>
            /// <param name="newId">The stage ID to assign to the cloned row.</param>
            /// <returns>A new <see cref="Row"/> with copied values and the specified ID.</returns>
            public Row CloneWithId(int newId)
            {
                return new Row
                {
                    Id = newId,
                    CostAP = CostAP,
                    EntryCostItemId = EntryCostItemId,
                    EntryCostItemCount = EntryCostItemCount,
                    TurnLimit = TurnLimit,
                    EnemyInitialStatModifiers = EnemyInitialStatModifiers is null
                        ? new List<StatModifier>()
                        : new List<StatModifier>(EnemyInitialStatModifiers),
                    Background = Background,
                    BGM = BGM,
                    Rewards = Rewards is null
                        ? new List<RewardData>()
                        : new List<RewardData>(Rewards),
                    DropItemMin = DropItemMin,
                    DropItemMax = DropItemMax,
                    FavRewards = FavRewards is null
                        ? new List<FavRewardData>()
                        : new List<FavRewardData>(FavRewards),
                    FavDropMin = FavDropMin,
                    FavDropMax = FavDropMax,
                };
            }
        }

        public StageSheet() : base(nameof(StageSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            base.AddRow(key, value);
        }
    }
}
