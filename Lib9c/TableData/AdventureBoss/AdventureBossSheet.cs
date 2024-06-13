using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.AdventureBoss
{
    [Serializable]
    public class AdventureBossSheet : Sheet<int, AdventureBossSheet.Row>
    {
        [Serializable]
        public class RewardAmountData
        {
            public string ItemType { get; }
            public int ItemId { get; }
            public int Amount { get; }

            public RewardAmountData(string itemType, int itemId, int amount)
            {
                ItemType = itemType;
                ItemId = itemId;
                Amount = amount;
            }
        }

        [Serializable]
        public class RewardRatioData
        {
            public string ItemType { get; }
            public int ItemId { get; }
            public int Ratio { get; }

            public RewardRatioData(string itemType, int itemId, int ratio)
            {
                ItemType = itemType;
                ItemId = itemId;
                Ratio = ratio;
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = TryParseInt(fields[0], out var id) ? id : 0;
                BossId = TryParseInt(fields[1], out var bossId) ? bossId : 0;
            }

            public int Id { get; private set; }
            public int BossId { get; private set; }
        }

        public AdventureBossSheet() : base(nameof(AdventureBossSheet))
        {
        }
    }
}
