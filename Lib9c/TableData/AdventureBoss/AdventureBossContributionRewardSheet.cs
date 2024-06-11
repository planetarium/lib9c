using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.AdventureBoss
{
    [Serializable]
    public class
        AdventureBossContributionRewardSheet : Sheet<int, AdventureBossContributionRewardSheet.Row>
    {
        [Serializable]
        public class RewardData
        {
            public string ItemType { get; }
            public int ItemId { get; }
            public int Ratio { get; }

            public RewardData(string itemType, int itemId, int ratio)
            {
                ItemType = itemType;
                ItemId = itemId;
                Ratio = ratio;
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key { get; }
            public int Id;
            public int AdventureBossId;
            public List<RewardData> Rewards;

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = TryParseInt(fields[0], out var id) ? id : 0;
                AdventureBossId = TryParseInt(fields[1], out var bossId) ? bossId : 0;
                Rewards = new List<RewardData>();
                for (var i = 0; i < 2; i++)
                {
                    var offset = 3 * i;
                    Rewards.Add(new RewardData(
                        fields[2 + offset],
                        TryParseInt(fields[3 + offset], out var itemId) ? itemId : 0,
                        TryParseInt(fields[4 + offset], out var ratio) ? ratio : 0
                    ));
                }
            }
        }

        public AdventureBossContributionRewardSheet(string name) : base(name)
        {
        }
    }
}
