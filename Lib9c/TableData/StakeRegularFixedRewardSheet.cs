using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.State;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    /// <summary>
    /// This sheet is used for setting the regular rewards for staking.
    /// The difference between this sheet and <see cref="StakeRegularRewardSheet"/> is that
    /// the <see cref="RewardInfo"/> of this sheet has a fixed count of reward.
    /// <para>CSV column order: level, required_gold, item_id, count[, tradable]</para>
    /// <para>
    /// The optional <c>tradable</c> column controls whether material items are created
    /// as tradable (<see cref="Nekoyume.Model.Item.TradableMaterial"/>) or non-tradable
    /// (<see cref="Nekoyume.Model.Item.Material"/>). Omitting the column defaults to
    /// <c>true</c>, preserving backward compatibility with existing CSV data.
    /// </para>
    /// </summary>
    [Serializable]
    public class StakeRegularFixedRewardSheet :
        Sheet<int, StakeRegularFixedRewardSheet.Row>,
        IStakeRewardSheet
    {
        [Serializable]
        public class RewardInfo
        {
            public readonly int ItemId;
            public readonly int Count;

            /// <summary>
            /// Whether the rewarded item is tradable.
            /// If <c>true</c>, materials are created as
            /// <see cref="Nekoyume.Model.Item.TradableMaterial"/>;
            /// otherwise they are created as non-tradable
            /// <see cref="Nekoyume.Model.Item.Material"/>.
            /// Defaults to <c>true</c> when the column is omitted in the CSV.
            /// </summary>
            public readonly bool Tradable;

            /// <summary>
            /// Initializes a <see cref="RewardInfo"/> from CSV field tokens.
            /// Expected order: item_id, count[, tradable].
            /// </summary>
            public RewardInfo(params string[] fields)
            {
                ItemId = ParseInt(fields[0]);
                Count = ParseInt(fields[1]);
                Tradable = fields.Length >= 3
                    ? ParseBool(fields[2], true)
                    : true;
            }

            /// <param name="itemId">The item ID to reward.</param>
            /// <param name="count">The fixed count of items to reward.</param>
            /// <param name="tradable">
            /// Whether the rewarded material item should be tradable.
            /// Defaults to <c>true</c>.
            /// </param>
            public RewardInfo(int itemId, int count, bool tradable = true)
            {
                ItemId = itemId;
                Count = count;
                Tradable = tradable;
            }

            protected bool Equals(RewardInfo other)
            {
                return ItemId == other.ItemId &&
                       Count == other.Count &&
                       Tradable == other.Tradable;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((RewardInfo) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((ItemId * 397) ^ Count) ^ Tradable.GetHashCode();
                }
            }
        }

        [Serializable]
        public class Row : SheetRow<int>, IStakeRewardRow
        {
            public override int Key => Level;

            public int Level { get; private set; }

            public long RequiredGold { get; private set; }

            public List<RewardInfo> Rewards { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Level = ParseInt(fields[0]);
                RequiredGold = ParseInt(fields[1]);
                var info = new RewardInfo(fields.Skip(2).ToArray());
                Rewards = new List<RewardInfo> {info};
            }
        }

        public StakeRegularFixedRewardSheet() : base(nameof(StakeRegularFixedRewardSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (!value.Rewards.Any())
            {
                return;
            }

            row.Rewards.Add(value.Rewards[0]);
        }

        public IReadOnlyList<IStakeRewardRow> OrderedRows => OrderedList;
    }
}
