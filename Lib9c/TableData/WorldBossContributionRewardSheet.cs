using System;
using System.Collections.Generic;
using System.Numerics;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    /// <summary>
    /// Represents the reward sheet for world boss contributions.
    /// </summary>
    public class WorldBossContributionRewardSheet : Sheet<int, WorldBossContributionRewardSheet.Row>
    {
        [Serializable]
        public class RewardModel
        {
            /// <summary>
            /// Gets or sets the quantity of the reward.
            /// </summary>
            public BigInteger Count;

            /// <summary>
            /// Gets or sets the ID of the item.
            /// </summary>
            public int ItemId;

            /// <summary>
            /// Gets or sets the ticker of the item.
            /// </summary>
            public string Ticker;

            /// <summary>
            /// Initializes a new instance of the <see cref="RewardModel"/> class.
            /// </summary>
            /// <param name="count">The quantity of the reward.</param>
            /// <param name="itemId">The ID of the item if reward is ItemBase.</param>
            /// <param name="ticker">The ticker of the item if reward is FungibleAssetValue.</param>
            public RewardModel(BigInteger count, int itemId, string ticker)
            {
                Count = count;
                ItemId = itemId;
                Ticker = ticker;
            }
        }

        /// <summary>
        /// Represents each row in the sheet.
        /// </summary>
        public class Row : SheetRow<int>
        {
            private const int MaxRewardCount = 100; // Maximum number of rewards

            /// <summary>
            /// Gets or sets the ID of the boss.
            /// </summary>
            public int BossId;

            /// <summary>
            /// Gets the list of rewards.
            /// </summary>
            public readonly List<RewardModel> Rewards = new ();

            /// <summary>
            /// Gets the key value of the row.
            /// </summary>
            public override int Key => BossId;

            /// <summary>
            /// Sets the row based on given fields.
            /// </summary>
            /// <param name="fields">The fields to set the row.</param>
            public override void Set(IReadOnlyList<string> fields)
            {
                BossId = ParseInt(fields[0]);
                for (var i = 0; i < MaxRewardCount; i++)
                {
                    var idx = 1 + i * 3;
                    if (fields.Count >= idx + 3 &&
                        TryParseBigInteger(fields[idx], out var count))
                    {
                        TryParseInt(fields[idx + 1], out var itemId);
                        Rewards.Add(new (count, itemId, fields[idx + 2]));
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorldBossContributionRewardSheet"/> class.
        /// </summary>
        public WorldBossContributionRewardSheet() : base(nameof(WorldBossContributionRewardSheet))
        {
        }
    }
}
