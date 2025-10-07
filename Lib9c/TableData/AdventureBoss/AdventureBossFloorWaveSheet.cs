using System;
using System.Collections.Generic;
using System.Linq;
using static Lib9c.TableData.TableExtensions;

namespace Lib9c.TableData.AdventureBoss
{
    [Serializable]
    public class AdventureBossFloorWaveSheet : Sheet<int, AdventureBossFloorWaveSheet.Row>
    {
        [Serializable]
        public class WaveData
        {
            public int Number { get; }
            public List<MonsterData> Monsters { get; }
            public bool HasBoss { get; }

            public WaveData(int number, List<MonsterData> monsters, bool hasBoss)
            {
                Number = number;
                Monsters = monsters;
                HasBoss = hasBoss;
            }
        }

        [Serializable]
        public class MonsterData
        {
            public int CharacterId { get; }
            public int Level { get; }
            public int Count { get; }

            public MonsterData(int characterId, int level, int count)
            {
                CharacterId = characterId;
                Level = level;
                Count = count;
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => FloorId;

            public int FloorId { get; private set; }
            public List<WaveData> Waves { get; private set; }
            public bool HasBoss { get; private set; }
            public List<int> TotalMonsterIds { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                FloorId = TryParseInt(fields[0], out var floorId) ? floorId : 0;
                Waves = new List<WaveData>();
                if (!TryParseInt(fields[1], out var wave))
                {
                    return;
                }

                var monsters = new List<MonsterData>();
                for (var i = 0; i < 4; i++)
                {
                    var offset = i * 3;
                    var characterId = TryParseInt(fields[2 + offset], out var outCharacterId)
                        ? outCharacterId
                        : 0;
                    if (characterId == 0)
                        break;

                    monsters.Add(new MonsterData(
                        characterId,
                        TryParseInt(fields[3 + offset], out var level) ? level : 0,
                        TryParseInt(fields[4 + offset], out var count) ? count : 0
                    ));
                }

                var isBoss = fields[14].Equals("1");
                Waves.Add(new WaveData(wave, monsters, isBoss));
            }

            public override void EndOfSheetInitialize()
            {
                Waves.Sort((left, right) =>
                {
                    if (left.Number > right.Number) return 1;
                    if (left.Number < right.Number) return -1;
                    return 0;
                });

                HasBoss = Waves.Any(wave => wave.HasBoss);
                TotalMonsterIds = new List<int>();
                TotalMonsterIds.AddRange(Waves.SelectMany(wave => wave.Monsters)
                    .Select(monster => monster.CharacterId)
                    .Distinct());
            }
        }

        public AdventureBossFloorWaveSheet() : base(nameof(AdventureBossFloorWaveSheet))
        {
        }
    }
}
