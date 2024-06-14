using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.AdventureBoss
{
    [Serializable]
    public class FloorSheet : Sheet<int, FloorSheet.Row>
    {
        [Serializable]
        public class RewardData
        {
            public int ItemId { get; }
            public decimal Ratio { get; }
            public int Min { get; }
            public int Max { get; }

            public RewardData(int itemId, decimal ratio, int min, int max)
            {
                ItemId = itemId;
                Ratio = ratio;
                Min = min;
                Max = max;
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

            public int DropItemMin { get; private set; }
            public int DropItemMax { get; private set; }
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
                        itemId,
                        TryParseDecimal(fields[16 + offset], out var ratio) ? ratio : 0m,
                        TryParseInt(fields[14 + offset], out var min) ? min : 0,
                        TryParseInt(fields[15 + offset], out var max) ? max : 0
                    ));
                }

                DropItemMin = TryParseInt(fields[27], out var dropMin) ? dropMin : 0;
                DropItemMax = TryParseInt(fields[28], out var dropMax) ? dropMax : 0;

                StageBuffSkillId = TryParseInt(fields[29], out var skillId) ? skillId : 0;
            }
        }

        public FloorSheet() : base(nameof(FloorSheet))
        {
        }
    }
}
