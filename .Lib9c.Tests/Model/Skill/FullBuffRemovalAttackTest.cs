namespace Lib9c.Tests.Model.Skill
{
    using System.Collections.Generic;
    using Bencodex.Types;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Skill.Arena;
    using Nekoyume.Model.Stat;
    using Nekoyume.TableData;
    using Xunit;

    public class FullBuffRemovalAttackTest
    {
        private readonly SkillSheet.Row _skillRow;

        public FullBuffRemovalAttackTest()
        {
            var skillDict = new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"id"] = (Integer)700016,
                [(Text)"elemental_type"] = (Text)"Normal",
                [(Text)"skill_type"] = (Text)"Attack",
                [(Text)"skill_category"] = (Text)"FullBuffRemovalAttack",
                [(Text)"skill_target_type"] = (Text)"Enemy",
                [(Text)"hit_count"] = (Integer)1,
                [(Text)"cooldown"] = (Integer)0,
                [(Text)"combo"] = (Bencodex.Types.Boolean)false,
            });
            _skillRow = new SkillSheet.Row(skillDict);
        }

        [Fact]
        public void SkillCategory_FullBuffRemovalAttack_Has_Value_20()
        {
            Assert.Equal(20, (int)SkillCategory.FullBuffRemovalAttack);
        }

        [Fact]
        public void SkillFactory_Get_Returns_FullBuffRemovalAttack()
        {
            var skill = SkillFactory.Get(
                _skillRow,
                100,
                50,
                0,
                StatType.NONE);

            Assert.IsType<FullBuffRemovalAttack>(skill);
            Assert.Equal(700016, skill.SkillRow.Id);
            Assert.Equal(
                SkillCategory.FullBuffRemovalAttack,
                skill.SkillRow.SkillCategory);
        }

        [Fact]
        public void SkillFactory_GetForArena_Returns_ArenaFullBuffRemovalAttack()
        {
            var skill = SkillFactory.GetForArena(
                _skillRow,
                100,
                50,
                0,
                StatType.NONE);

            Assert.IsType<ArenaFullBuffRemovalAttack>(skill);
            Assert.Equal(700016, skill.SkillRow.Id);
        }

        [Fact]
        public void SkillSheet_Row_Serialize_Deserialize_RoundTrip()
        {
            var serialized = _skillRow.Serialize();
            var deserialized = SkillSheet.Row.Deserialize(serialized);

            Assert.Equal(
                SkillCategory.FullBuffRemovalAttack,
                deserialized.SkillCategory);
            Assert.Equal(_skillRow.Id, deserialized.Id);
            Assert.Equal(_skillRow.SkillType, deserialized.SkillType);
        }

        [Fact]
        public void Skill_Serialize_Deserialize_RoundTrip()
        {
            var original = SkillFactory.Get(
                _skillRow,
                200,
                40,
                10,
                StatType.ATK);
            var serialized = original.Serialize();
            var deserialized =
                SkillFactory.DeserializeFromList((List)serialized);

            Assert.IsType<FullBuffRemovalAttack>(deserialized);
            Assert.Equal(original.SkillRow.Id, deserialized.SkillRow.Id);
            Assert.Equal(original.Power, deserialized.Power);
            Assert.Equal(original.Chance, deserialized.Chance);
            Assert.Equal(
                original.StatPowerRatio,
                deserialized.StatPowerRatio);
            Assert.Equal(
                original.ReferencedStatType,
                deserialized.ReferencedStatType);
        }
    }
}
