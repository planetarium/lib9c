namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using Lib9c.Model.Character;
    using Priority_Queue;
    using Xunit;

    public class SimplePriorityQueueTest
    {
        [Theory]
        [InlineData(1000000, 1)]
        [InlineData(1000000, 0.000001)]
        public void DeterministicFirstDequeue(int loopCount, decimal spd)
        {
            var results = new List<int>();
            for (var i = 0; i < loopCount; i++)
            {
                var queue = new SimplePriorityQueue<int, decimal>();
                queue.Enqueue(0, spd);
                queue.Enqueue(1, spd);
                queue.Enqueue(2, spd);

                Assert.True(queue.TryDequeue(out var index));
                results.Add(index);
            }

            for (var i = 0; i < results.Count - 1; i++)
            {
                Assert.Equal(results[i], results[i + 1]);
            }
        }

        [Theory]
        [InlineData(1000000, 1)]
        [InlineData(1000000, 0.000001)]
        public void DeterministicIterateDequeueAndEnqueue(int loopCount, decimal spd)
        {
            var results1 = new List<int>();
            var results2 = new List<int>();
            for (var i = 0; i < 2; i++)
            {
                var targetResults = i == 0
                    ? results1
                    : results2;

                var queue = new SimplePriorityQueue<int, decimal>();
                queue.Enqueue(0, spd);
                queue.Enqueue(1, spd);
                queue.Enqueue(2, spd);

                for (var j = 0; j < loopCount; j++)
                {
                    Assert.True(queue.TryDequeue(out var index));
                    targetResults.Add(index);

                    queue.Enqueue(index, spd);
                }
            }

            Assert.Equal(results1, results2);
        }

        [Theory]
        [InlineData(1000000, 1)]
        [InlineData(1000000, 0.000001)]
        public void DeterministicIterateDequeueAndEnqueueWithMultiplier(int loopCount, decimal spd)
        {
            var results1 = new List<int>();
            var results2 = new List<int>();
            const decimal multiplier = 0.6m;
            for (var i = 0; i < 2; i++)
            {
                var targetResults = i == 0
                    ? results1
                    : results2;

                var queue = new SimplePriorityQueue<int, decimal>();
                queue.Enqueue(0, spd * multiplier);
                queue.Enqueue(1, spd * multiplier);
                queue.Enqueue(2, spd * multiplier);

                for (var j = 0; j < loopCount; j++)
                {
                    Assert.True(queue.TryDequeue(out var index));
                    targetResults.Add(index);

                    queue.Enqueue(index, spd * multiplier);
                }
            }

            Assert.Equal(results1, results2);
        }

        [Theory]
        [InlineData(1000000, 1)]
        [InlineData(1000000, 0.000001)]
        public void DeterministicIterateDequeueAndEnqueueWithDivisor(int loopCount, decimal spd)
        {
            var results1 = new List<int>();
            var results2 = new List<int>();
            const decimal divisor = 987654321m;
            for (var i = 0; i < 2; i++)
            {
                var targetResults = i == 0
                    ? results1
                    : results2;

                var queue = new SimplePriorityQueue<int, decimal>();
                queue.Enqueue(0, spd / divisor);
                queue.Enqueue(1, spd / divisor);
                queue.Enqueue(2, spd / divisor);

                for (var j = 0; j < loopCount; j++)
                {
                    Assert.True(queue.TryDequeue(out var index));
                    targetResults.Add(index);

                    queue.Enqueue(index, spd / divisor);
                }
            }

            Assert.Equal(results1, results2);
        }

        [Theory]
        [InlineData(1000000, 1)]
        [InlineData(1000000, 0.000001)]
        public void DeterministicIterateDequeueAndUpdateAndEnqueue(int loopCount, decimal spd)
        {
            var results1 = new List<int>();
            var results2 = new List<int>();
            for (var i = 0; i < 2; i++)
            {
                var targetResults = i == 0
                    ? results1
                    : results2;

                var queue = new SimplePriorityQueue<int, decimal>();
                queue.Enqueue(0, spd);
                queue.Enqueue(1, spd);
                queue.Enqueue(2, spd);

                for (var j = 0; j < loopCount; j++)
                {
                    Assert.True(queue.TryDequeue(out var index));
                    targetResults.Add(index);

                    foreach (var otherIndex in queue)
                    {
                        var priority = queue.GetPriority(otherIndex);
                        queue.UpdatePriority(otherIndex, priority);
                    }

                    queue.Enqueue(index, spd);
                }
            }

            Assert.Equal(results1, results2);
        }

        [Theory]
        [InlineData(1000000, 1)]
        [InlineData(1000000, 0.000001)]
        public void GuidDeterministicIterateDequeueAndUpdateAndEnqueue(int loopCount, decimal spd)
        {
            var results1 = new List<Guid>();
            var results2 = new List<Guid>();
            var guids = new[]
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
            };

            for (var i = 0; i < 2; i++)
            {
                var targetResults = i == 0
                    ? results1
                    : results2;

                var queue = new SimplePriorityQueue<Guid, decimal>();
                for (var j = 0; j < guids.Length; j++)
                {
                    queue.Enqueue(guids[j], spd);
                }

                for (var j = 0; j < loopCount; j++)
                {
                    Assert.True(queue.TryDequeue(out var guid));
                    targetResults.Add(guid);

                    foreach (var otherGuid in queue)
                    {
                        var priority = queue.GetPriority(otherGuid);
                        queue.UpdatePriority(otherGuid, priority);
                    }

                    queue.Enqueue(guid, spd);
                }
            }

            Assert.Equal(results1, results2);
        }

        [Theory]
        [InlineData(1000000, 1)]
        [InlineData(1000000, 0.000001)]
        public void PlayerDeterministicIterateDequeueAndUpdateAndEnqueue(int loopCount, decimal spd)
        {
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var players = new[]
            {
                new Player(
                    1,
                    tableSheets.CharacterSheet,
                    tableSheets.CharacterLevelSheet,
                    tableSheets.EquipmentItemSetEffectSheet),
                new Player(
                    2,
                    tableSheets.CharacterSheet,
                    tableSheets.CharacterLevelSheet,
                    tableSheets.EquipmentItemSetEffectSheet),
                new Player(
                    3,
                    tableSheets.CharacterSheet,
                    tableSheets.CharacterLevelSheet,
                    tableSheets.EquipmentItemSetEffectSheet),
            };

            var results1 = new List<Player>();
            var results2 = new List<Player>();
            for (var i = 0; i < 2; i++)
            {
                var targetResults = i == 0
                    ? results1
                    : results2;

                var queue = new SimplePriorityQueue<Player, decimal>();
                for (var j = 0; j < players.Length; j++)
                {
                    queue.Enqueue(players[j], spd);
                }

                for (var j = 0; j < loopCount; j++)
                {
                    Assert.True(queue.TryDequeue(out var player));
                    targetResults.Add(player);

                    foreach (var otherPlayer in queue)
                    {
                        var priority = queue.GetPriority(otherPlayer);
                        queue.UpdatePriority(otherPlayer, priority);
                    }

                    queue.Enqueue(player, spd);
                }
            }

            Assert.Equal(results1, results2);
        }
    }
}
