using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;
using RewardData = Nekoyume.TableData.AdventureBoss.AdventureBossSheet.RewardAmountData;

namespace Nekoyume.TableData.AdventureBoss
{
    [Serializable]
    public class AdventureBossFloorFirstRewardSheet
        : Sheet<int, AdventureBossFloorFirstRewardSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => FloorId;
            public int FloorId;
            public List<RewardData> Rewards;

            public override void Set(IReadOnlyList<string> fields)
            {
                FloorId = TryParseInt(fields[0], out var stageId) ? stageId : 0;
                Rewards = new List<RewardData>();
                for (var i = 0; i < 2; i++)
                {
                    var offset = 3 * i;
                    Rewards.Add(new RewardData(
                        fields[1 + offset],
                        TryParseInt(fields[2 + offset], out var itemId) ? itemId : 0,
                        TryParseInt(fields[3 + offset], out var amount) ? amount : 0
                    ));
                }
            }
        }

        public AdventureBossFloorFirstRewardSheet()
            : base(nameof(AdventureBossFloorFirstRewardSheet))
        {
        }
    }
}
