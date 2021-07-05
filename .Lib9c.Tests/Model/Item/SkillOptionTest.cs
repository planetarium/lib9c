namespace Lib9c.Tests.Model.Item
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Skill;
    using Nekoyume.TableData;
    using Xunit;

    public class SkillOptionTest
    {
        private readonly SkillSheet _skillSheet;

        public SkillOptionTest()
        {
            var csv = TableSheetsImporter.ImportSheets()[nameof(SkillSheet)];
            _skillSheet = new SkillSheet();
            _skillSheet.Set(csv);
        }

        public static IEnumerable<Skill> CreateAllSkills(
            SkillSheet skillSheet,
            int maxPower = int.MaxValue / 100,
            int maxChance = int.MaxValue / 100)
        {
            var random = new Random(DateTime.UtcNow.Millisecond);
            return skillSheet.OrderedList.Select(row => SkillFactory.Get(
                row,
                random.Next(0, maxPower),
                random.Next(0, maxChance)));
        }

        [Fact]
        public void Serialize()
        {
            foreach (var skill in CreateAllSkills(_skillSheet))
            {
                foreach (var grade in Enumerable.Range(1, 10))
                {
                    var option = new SkillOption(grade, skill);
                    var serialized = option.Serialize();
                    var deserialized = new SkillOption(serialized);
                    Assert.Equal(option.Grade, deserialized.Grade);
                    Assert.Equal(option.Skill, deserialized.Skill);
                }
            }
        }

        [Fact]
        public void Enhance()
        {
            var random = new Random(DateTime.UtcNow.Millisecond);
            foreach (var skill in CreateAllSkills(_skillSheet))
            {
                var ratio = (decimal)random.NextDouble();
                var fromChance = skill.Chance;
                var fromPower = skill.Power;
                var option = new SkillOption(default, skill);
                option.Enhance(ratio);
                Assert.Equal(decimal.ToInt32(fromChance * (1 + ratio)), option.Skill.Chance);
                Assert.Equal(decimal.ToInt32(fromPower * (1 + ratio)), option.Skill.Power);
            }
        }
    }
}
