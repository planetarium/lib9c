using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.AdventureBoss
{
    [Serializable]
    public class AdventureBossFloorFirstReward : Sheet<int, AdventureBossFloorFirstReward.Row>
    {
        [Serializable]
        public class RewardData
        {
            public string ItemType;
            public int ItemId;
            public int Amount;

            public RewardData(string itemType, int itemId, int amount)
            {
                ItemType = itemType;
                ItemId = itemId;
                Amount = amount;
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id;
            public int StageId;
            public List<RewardData> Rewards;

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = TryParseInt(fields[0], out var id) ? id : 0;
                StageId = TryParseInt(fields[1], out var stageId) ? stageId : 0;
                Rewards = new List<RewardData>();
                for (var i = 0; i < 3; i++)
                {
                    var offset = 3 * i;
                    Rewards.Add(new RewardData(
                        fields[2 + offset],
                        TryParseInt(fields[3 + offset], out var itemId) ? itemId : 0,
                        TryParseInt(fields[4 + offset], out var amount) ? amount : 0
                    ));
                }
            }
        }

        public AdventureBossFloorFirstReward(string name) : base(name)
        {
        }
    }
}
