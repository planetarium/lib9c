using System;
using System.Collections.Generic;
using Lib9c.Model.Stat;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.AdventureBoss
{
    [Serializable]
    public class AdventureBossFloorSheet : Sheet<int, AdventureBossFloorSheet.Row>
    {
        [Serializable]
        public class RewardData
        {
            public string ItemType { get; }
            public int ItemId { get; }
            public int Ratio { get; }
            public int Min { get; }
            public int Max { get; }

            public RewardData(string itemType, int itemId, int min, int max, int ratio)
            {
                ItemType = itemType;
                ItemId = itemId;
                Min = min;
                Max = max;
                Ratio = ratio;
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            private const string DefaultBGM = "bgm_stage_green";

            public override int Key => Id;
            public int Id { get; private set; }
            public int AdventureBossId { get; private set; }
            public int Floor { get; private set; }
            public int TurnLimit { get; private set; }
            public List<StatModifier> EnemyInitialStatModifiers { get; private set; }
            public string Background { get; private set; }
            public string BGM { get; private set; }
            public List<RewardData> Rewards { get; private set; }

            public int MinDropItem { get; private set; }
            public int MaxDropItem { get; private set; }
            public int StageBuffSkillId { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = TryParseInt(fields[0], out var id) ? id : 0;
                AdventureBossId = TryParseInt(fields[1], out var bossId) ? bossId : 0;
                Floor = TryParseInt(fields[2], out var floor) ? floor : 0;
                TurnLimit = TryParseInt(fields[3], out var turnLimit) ? turnLimit : 0;
                EnemyInitialStatModifiers = new List<StatModifier>();
                for (var i = 0; i < 6; i++)
                {
                    if (!TryParseInt(fields[4 + i], out var option) || option == 0)
                        continue;

                    switch (i)
                    {
                        case 0:
                            EnemyInitialStatModifiers.Add(new StatModifier(StatType.HP,
                                StatModifier.OperationType.Percentage, option));
                            break;
                        case 1:
                            EnemyInitialStatModifiers.Add(new StatModifier(StatType.ATK,
                                StatModifier.OperationType.Percentage, option));
                            break;
                        case 2:
                            EnemyInitialStatModifiers.Add(new StatModifier(StatType.DEF,
                                StatModifier.OperationType.Percentage, option));
                            break;
                        case 3:
                            EnemyInitialStatModifiers.Add(new StatModifier(StatType.CRI,
                                StatModifier.OperationType.Percentage, option));
                            break;
                        case 4:
                            EnemyInitialStatModifiers.Add(new StatModifier(StatType.HIT,
                                StatModifier.OperationType.Percentage, option));
                            break;
                        case 5:
                            EnemyInitialStatModifiers.Add(new StatModifier(StatType.SPD,
                                StatModifier.OperationType.Percentage, option));
                            break;
                    }
                }

                Background = fields[10];
                BGM = string.IsNullOrEmpty(fields[11])
                    ? DefaultBGM
                    : fields[11];
                Rewards = new List<RewardData>();
                for (var i = 0; i < 3; i++)
                {
                    var offset = i * 5;
                    if (!TryParseInt(fields[13 + offset], out var itemId))
                        continue;

                    Rewards.Add(new RewardData(
                        fields[12 + offset],
                        itemId,
                        TryParseInt(fields[14 + offset], out var min) ? min : 0,
                        TryParseInt(fields[15 + offset], out var max) ? max : 0,
                        TryParseInt(fields[16 + offset], out var ratio) ? ratio : 0
                    ));
                }

                MinDropItem = TryParseInt(fields[27], out var minDrop) ? minDrop : 0;
                MaxDropItem = TryParseInt(fields[28], out var maxDrop) ? maxDrop : 0;
                StageBuffSkillId = TryParseInt(fields[29], out var skillId) ? skillId : 0;
            }
        }

        public AdventureBossFloorSheet() : base(nameof(AdventureBossFloorSheet))
        {
        }
    }
}
