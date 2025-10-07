using System;
using System.Collections.Generic;
using static Lib9c.TableData.TableExtensions;
using RewardData = Lib9c.TableData.AdventureBoss.AdventureBossSheet.RewardRatioData;

namespace Lib9c.TableData.AdventureBoss
{
    [Serializable]
    public class
        AdventureBossContributionRewardSheet : Sheet<int, AdventureBossContributionRewardSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id;
            public int AdventureBossId;
            public List<RewardData> Rewards;

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = TryParseInt(fields[0], out var id) ? id : 0;
                AdventureBossId = TryParseInt(fields[1], out var bossId) ? bossId : 0;
                Rewards = new List<RewardData>();

                var offset = 0;
                while (fields.Count >= 4 + offset)
                {
                    var itemType = fields[2 + offset];
                    var itemId = TryParseInt(fields[3 + offset], out var iid) ? iid : 0;
                    var ratio =
                        TryParseInt(fields[4 + offset], out var rt) ? rt : 0;

                    if (itemType != "" && itemId != 0 && ratio != 0)
                    {
                        Rewards.Add(new RewardData(itemType, itemId, ratio));
                    }

                    offset += 3;
                }
            }
        }

        public AdventureBossContributionRewardSheet()
            : base(nameof(AdventureBossContributionRewardSheet))
        {
        }
    }
}
