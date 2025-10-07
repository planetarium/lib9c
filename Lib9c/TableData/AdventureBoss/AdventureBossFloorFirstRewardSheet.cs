using System;
using System.Collections.Generic;
using Lib9c.Action;
using static Lib9c.TableData.TableExtensions;
using RewardData = Lib9c.TableData.AdventureBoss.AdventureBossSheet.RewardAmountData;

namespace Lib9c.TableData.AdventureBoss
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
                    if (!string.IsNullOrWhiteSpace(fields[1 + offset]))
                    {
                        Rewards.Add(new RewardData(
                            fields[1 + offset],
                            TryParseInt(fields[2 + offset], out var itemId)
                                ? itemId
                                : throw new FailedLoadSheetException("Missing Item Id"),
                            TryParseInt(fields[3 + offset], out var amount)
                                ? amount
                                : throw new FailedLoadSheetException("Missing Item amount")
                        ));
                    }
                }
            }
        }

        public AdventureBossFloorFirstRewardSheet()
            : base(nameof(AdventureBossFloorFirstRewardSheet))
        {
        }
    }
}
