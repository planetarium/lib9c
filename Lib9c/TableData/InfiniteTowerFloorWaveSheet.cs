using System;
using System.Collections.Generic;
using System.Linq;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    /// <summary>
    /// Infinite tower floor wave sheet for managing wave configurations.
    /// Supports multiple monster types per wave, similar to StageWaveSheet.
    /// </summary>
    [Serializable]
    public class InfiniteTowerFloorWaveSheet : Sheet<int, InfiniteTowerFloorWaveSheet.Row>
    {
        /// <summary>
        /// Initializes a new instance of the InfiniteTowerFloorWaveSheet class.
        /// </summary>
        public InfiniteTowerFloorWaveSheet() : base(nameof(InfiniteTowerFloorWaveSheet)) { }

        /// <summary>
        /// Represents a single wave within a floor, containing multiple monster types.
        /// </summary>
        [Serializable]
        public class WaveData
        {
            /// <summary>
            /// Gets the wave number within the floor.
            /// </summary>
            public int Number { get; }

            /// <summary>
            /// Gets the list of monster types in this wave.
            /// </summary>
            public List<MonsterData> Monsters { get; }

            /// <summary>
            /// Gets whether this wave contains a boss monster.
            /// </summary>
            public bool HasBoss { get; }

            /// <summary>
            /// Initializes a new instance of the WaveData class.
            /// </summary>
            /// <param name="number">The wave number.</param>
            /// <param name="monsters">List of monsters in this wave.</param>
            /// <param name="hasBoss">Whether this wave has a boss.</param>
            public WaveData(int number, List<MonsterData> monsters, bool hasBoss)
            {
                Number = number;
                Monsters = monsters;
                HasBoss = hasBoss;
            }
        }

        /// <summary>
        /// Represents a single monster type within a wave.
        /// </summary>
        [Serializable]
        public class MonsterData
        {
            /// <summary>
            /// Gets the character ID of the monster.
            /// </summary>
            public int CharacterId { get; }

            /// <summary>
            /// Gets the level of the monster.
            /// </summary>
            public int Level { get; }

            /// <summary>
            /// Gets the number of monsters of this type in the wave.
            /// </summary>
            public int Count { get; }

            /// <summary>
            /// Initializes a new instance of the MonsterData class.
            /// </summary>
            /// <param name="characterId">The character ID.</param>
            /// <param name="level">The monster level.</param>
            /// <param name="count">The number of monsters.</param>
            public MonsterData(int characterId, int level, int count)
            {
                CharacterId = characterId;
                Level = level;
                Count = count;
            }
        }

        /// <summary>
        /// Represents a row in the InfiniteTowerFloorWaveSheet containing wave data for a specific floor.
        /// </summary>
        [Serializable]
        public class Row : SheetRow<int>
        {
            /// <summary>
            /// Gets the floor ID as the key for this row.
            /// </summary>
            public override int Key => FloorId;

            /// <summary>
            /// Gets the floor ID this row represents.
            /// </summary>
            public int FloorId { get; private set; }

            /// <summary>
            /// Gets the list of waves for this floor.
            /// </summary>
            public List<WaveData> Waves { get; private set; }

            /// <summary>
            /// Gets whether any wave in this floor contains a boss monster.
            /// </summary>
            public bool HasBoss { get; private set; }

            /// <summary>
            /// Gets the list of all unique monster character IDs across all waves in this floor.
            /// </summary>
            public List<int> TotalMonsterIds { get; private set; }

            /// <summary>
            /// Sets the row data from CSV fields.
            /// Parses multiple monster types per wave from the CSV data.
            /// </summary>
            /// <param name="fields">CSV field data.</param>
            public override void Set(IReadOnlyList<string> fields)
            {
                FloorId = TryParseInt(fields[1], out var floorId) ? floorId : 0;
                Waves = new List<WaveData>();
                if (!TryParseInt(fields[2], out var wave))
                    return;

                var monsters = new List<MonsterData>();
                for (var i = 0; i < 4; i++)
                {
                    var offset = i * 3;
                    var characterId = TryParseInt(fields[3 + offset], out var outCharacterId) ? outCharacterId : 0;
                    if (characterId == 0)
                        break;

                    monsters.Add(new MonsterData(
                        characterId,
                        TryParseInt(fields[4 + offset], out var level) ? level : 0,
                        TryParseInt(fields[5 + offset], out var count) ? count : 0
                    ));
                }

                var isBoss = fields[15].Equals("1");
                Waves.Add(new WaveData(wave, monsters, isBoss));
            }

            /// <summary>
            /// Performs post-initialization tasks after all rows have been loaded.
            /// Sorts waves by number and populates derived properties.
            /// </summary>
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

        /// <summary>
        /// Adds a row to the sheet, merging waves if the floor already exists.
        /// </summary>
        /// <param name="key">The floor ID key.</param>
        /// <param name="value">The row data to add.</param>
        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);
                return;
            }

            if (value.Waves.Count == 0)
                return;

            row.Waves.Add(value.Waves[0]);
        }
    }
}
