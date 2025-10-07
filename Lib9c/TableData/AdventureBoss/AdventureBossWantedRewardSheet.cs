using System;
using System.Collections.Generic;
using static Lib9c.TableData.TableExtensions;
using RewardData = Lib9c.TableData.AdventureBoss.AdventureBossSheet.RewardRatioData;

namespace Lib9c.TableData.AdventureBoss
{
    [Serializable]
    public class AdventureBossWantedRewardSheet : Sheet<int, AdventureBossWantedRewardSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;

            public int Id;
            public int AdventureBossId;
            public RewardData FixedReward;
            public List<RewardData> RandomRewards;

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = TryParseInt(fields[0], out var id) ? id : 0;
                AdventureBossId = TryParseInt(fields[1], out var bossId) ? bossId : 0;
                FixedReward = new RewardData(
                    fields[2],
                    TryParseInt(fields[3], out var fixedId) ? fixedId : 0,
                    0 // Fixed reward does not have candidates.
                );

                RandomRewards = new List<RewardData>();
                var offset = 0;
                while (fields.Count >= 6 + offset)
                {
                    var itemType = fields[4 + offset];
                    var itemId = TryParseInt(fields[5 + offset], out var iid) ? iid : 0;
                    var ratio = TryParseInt(fields[6 + offset], out var rt) ? rt : 0;
                    if (itemType != "" && itemId != 0 && ratio != 0)
                    {
                        RandomRewards.Add(new RewardData(itemType, itemId, ratio));
                    }

                    offset += 3;
                }
            }
        }

        public AdventureBossWantedRewardSheet() : base(nameof(AdventureBossWantedRewardSheet))
        {
        }
    }
}
