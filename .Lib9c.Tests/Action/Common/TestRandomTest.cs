namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;
    using Xunit.Abstractions;

    public class TestRandomTest
    {
        private const int RandomUpperBound = 100;

        private readonly ITestOutputHelper _testOutputHelper;

        public TestRandomTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData(0, 1, 10)]
        [InlineData(0, 1, 1_000)]
        [InlineData(0, 1, 1_000_000)]
        [InlineData(0, 2, 10)]
        [InlineData(0, 2, 1_000)]
        [InlineData(0, 2, 1_000_000)]
        [InlineData(0, 3, 10)]
        [InlineData(0, 3, 1_000)]
        [InlineData(0, 3, 1_000_000)]
        [InlineData(0, 4, 10)]
        [InlineData(0, 4, 1_000)]
        [InlineData(0, 4, 1_000_000)]
        public void SingleSeed_SingleRandom_Test(int randomSeed, int dictCount, int valueCountForEachDict)
        {
            var seedRandom = new TestRandom(randomSeed);
            var random = new TestRandom(seedRandom.Next());

            const int randomUpperBound = 100;
            var dictList = new List<Dictionary<int, int>>();
            for (var i = 0; i < dictCount; i++)
            {
                var dict = new Dictionary<int, int>();
                for (var j = 0; j < randomUpperBound; j++)
                {
                    dict[j] = 0;
                }

                dictList.Add(dict);
            }

            var loopCount = dictCount * valueCountForEachDict;
            for (var i = 0; i < loopCount; i++)
            {
                var index = i % dictCount;
                var dict = dictList[index];
                dict[random.Next(randomUpperBound)]++;
            }

            var pairValueCount = valueCountForEachDict / randomUpperBound;
            for (var i = 0; i < dictList.Count; i++)
            {
                var dict = dictList[i];
                Print(i, dict, valueCountForEachDict, pairValueCount);
            }
        }

        [Theory]
        [InlineData(0, 1, 10)]
        [InlineData(0, 1, 1_000)]
        [InlineData(0, 1, 1_000_000)]
        [InlineData(0, 2, 10)]
        [InlineData(0, 2, 1_000)]
        [InlineData(0, 2, 1_000_000)]
        [InlineData(0, 3, 10)]
        [InlineData(0, 3, 1_000)]
        [InlineData(0, 3, 1_000_000)]
        [InlineData(0, 4, 10)]
        [InlineData(0, 4, 1_000)]
        [InlineData(0, 4, 1_000_000)]
        public void SingleSeed_ManyRandom_Test(int randomSeed, int dictCount, int valueCountForEachDict)
        {
            var seedRandom = new TestRandom(randomSeed);

            var tupleList = new List<(Dictionary<int, int>, TestRandom)>();
            for (var i = 0; i < dictCount; i++)
            {
                var dict = new Dictionary<int, int>();
                for (var j = 0; j < RandomUpperBound; j++)
                {
                    dict[j] = 0;
                }

                tupleList.Add((dict, new TestRandom(seedRandom.Next())));
            }

            var loopCount = dictCount * valueCountForEachDict;
            for (var i = 0; i < loopCount; i++)
            {
                var index = i % dictCount;
                var (dict, random) = tupleList[index];
                dict[random.Next(RandomUpperBound)]++;
            }

            var pairValueCount = valueCountForEachDict / RandomUpperBound;
            for (var i = 0; i < tupleList.Count; i++)
            {
                var (dict, _) = tupleList[i];
                Print(i, dict, valueCountForEachDict, pairValueCount);
            }
        }

        private void Print(int index, Dictionary<int, int> dict, int valueCountForEachDict, int pairValueCount)
        {
            var differSum = dict.Sum(pair => Math.Abs(pair.Value - pairValueCount));
            var value = (float)differSum / RandomUpperBound / valueCountForEachDict;
            _testOutputHelper.WriteLine($"[{index}] y축 평균 오차 비율: {value:P4}");

            const int middleValue = RandomUpperBound / 2;
            var middleSum = dict.Where(pair => pair.Key >= middleValue).Sum(pair => pair.Value);
            value = (float)middleSum / valueCountForEachDict;
            _testOutputHelper.WriteLine($"[{index}] x축 중간값 좌우 편차 비율: {Math.Abs(value * 2 - 1f):P4}");
            var comment = $"(스킬 {valueCountForEachDict}번 중에 {valueCountForEachDict * value}번 발동)";
            _testOutputHelper.WriteLine($"[{index}] x축 중간값 이상 비율: {value:P4} {comment}");
        }
    }
}
