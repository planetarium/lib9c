namespace Lib9c.Tests.Model.Skill
{
    using System.Collections.Generic;
    using Bencodex.Types;
    using Lib9c.Model.Elemental;
    using Lib9c.Model.Skill;
    using Lib9c.Model.Stat;
    using Lib9c.Model.State;
    using Lib9c.TableData.Skill;
    using Xunit;

    public class SkillSerializationTest
    {
        private readonly SkillSheet.Row _skillRow;

        public SkillSerializationTest()
        {
            var skillDict = new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"id"] = (Integer)1,
                [(Text)"elemental_type"] = (Text)"Normal",
                [(Text)"skill_type"] = (Text)"Attack",
                [(Text)"skill_category"] = (Text)"NormalAttack",
                [(Text)"skill_target_type"] = (Text)"Enemy",
                [(Text)"hit_count"] = (Integer)1,
                [(Text)"cooldown"] = (Integer)0,
                [(Text)"combo"] = (Bencodex.Types.Boolean)false,
            });
            _skillRow = new SkillSheet.Row(skillDict);
        }

        // Legacy serialize for test
        public static IValue LegacySerializeSkill(Lib9c.Model.Skill.Skill skill)
        {
            var dict = new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"skillRow"] = LegacySerializeSkillRow(skill.SkillRow),
                [(Text)"power"] = skill.Power.Serialize(),
                [(Text)"chance"] = skill.Chance.Serialize(),
            });

            if (skill.StatPowerRatio != default && skill.ReferencedStatType != StatType.NONE)
            {
                dict = dict.Add("stat_power_ratio", skill.StatPowerRatio.Serialize())
                    .Add("referenced_stat_type", skill.ReferencedStatType.Serialize());
            }

            return dict;
        }

        public static IValue LegacySerializeSkillRow(SkillSheet.Row skillRow)
        {
            var dict = Bencodex.Types.Dictionary.Empty
                    .Add("id", skillRow.Id)
                    .Add("elemental_type", skillRow.ElementalType.ToString())
                    .Add("skill_type", skillRow.SkillType.ToString())
                    .Add("skill_category", skillRow.SkillCategory.ToString())
                    .Add("skill_target_type", skillRow.SkillTargetType.ToString())
                    .Add("hit_count", skillRow.HitCount)
                    .Add("cooldown", skillRow.Cooldown)
                    .Add("combo", skillRow.Combo)
                ;
            return dict;
        }

        [Fact]
        public void SkillSheet_Row_Serialize_ToList()
        {
            // Arrange
            var skillRow = _skillRow;

            // Act
            var serialized = skillRow.Serialize();

            // Assert
            Assert.IsType<List>(serialized);
            var list = (List)serialized;
            Assert.Equal(8, list.Count);
            Assert.Equal(1, (int)(Integer)list[0]);
            Assert.Equal((int)ElementalType.Normal, (int)(Integer)list[1]);
            Assert.Equal((int)SkillType.Attack, (int)(Integer)list[2]);
            Assert.Equal((int)SkillCategory.NormalAttack, (int)(Integer)list[3]);
            Assert.Equal((int)SkillTargetType.Enemy, (int)(Integer)list[4]);
            Assert.Equal(1, (int)(Integer)list[5]);
            Assert.Equal(0, (int)(Integer)list[6]);
            Assert.False(list[7].ToBoolean());
        }

        [Fact]
        public void SkillSheet_Row_Deserialize_FromList()
        {
            // Arrange
            var originalSkillRow = _skillRow;
            var serialized = originalSkillRow.Serialize();

            // Act
            var deserialized = SkillSheet.Row.Deserialize(serialized);

            // Assert
            Assert.Equal(originalSkillRow.Id, deserialized.Id);
            Assert.Equal(originalSkillRow.ElementalType, deserialized.ElementalType);
            Assert.Equal(originalSkillRow.SkillType, deserialized.SkillType);
            Assert.Equal(originalSkillRow.SkillCategory, deserialized.SkillCategory);
            Assert.Equal(originalSkillRow.SkillTargetType, deserialized.SkillTargetType);
            Assert.Equal(originalSkillRow.HitCount, deserialized.HitCount);
            Assert.Equal(originalSkillRow.Cooldown, deserialized.Cooldown);
            Assert.Equal(originalSkillRow.Combo, deserialized.Combo);
        }

        [Fact]
        public void SkillSheet_Row_Deserialize_FromDictionary_BackwardCompatibility()
        {
            // Arrange
            var skillDict = new Dictionary<IKey, IValue>
            {
                [(Text)"id"] = (Integer)1,
                [(Text)"elemental_type"] = (Text)"Normal",
                [(Text)"skill_type"] = (Text)"Attack",
                [(Text)"skill_category"] = (Text)"NormalAttack",
                [(Text)"skill_target_type"] = (Text)"Enemy",
                [(Text)"hit_count"] = (Integer)1,
                [(Text)"cooldown"] = (Integer)0,
                [(Text)"combo"] = (Bencodex.Types.Boolean)false,
            };
            var dict = new Bencodex.Types.Dictionary(skillDict);

            // Act
            var deserialized = SkillSheet.Row.Deserialize(dict);

            // Assert
            Assert.Equal(1, deserialized.Id);
            Assert.Equal(ElementalType.Normal, deserialized.ElementalType);
            Assert.Equal(SkillType.Attack, deserialized.SkillType);
            Assert.Equal(SkillCategory.NormalAttack, deserialized.SkillCategory);
            Assert.Equal(SkillTargetType.Enemy, deserialized.SkillTargetType);
            Assert.Equal(1, deserialized.HitCount);
            Assert.Equal(0, deserialized.Cooldown);
            Assert.False(deserialized.Combo);
        }

        [Fact]
        public void Skill_Serialize_WithListBasedSkillRow()
        {
            // Arrange
            var skill = SkillFactory.Get(_skillRow, 100, 50, 0, StatType.NONE);

            // Act
            var serialized = skill.Serialize();

            // Assert
            Assert.IsType<List>(serialized);
            var list = (List)serialized;
            Assert.Equal(3, list.Count);

            // First element should be the skill row (also a List)
            Assert.IsType<List>(list[0]);
            var skillRowList = (List)list[0];
            Assert.Equal(8, skillRowList.Count);

            Assert.Equal(100, list[1].ToInteger());
            Assert.Equal(50, list[2].ToInteger());
        }

        [Fact]
        public void Skill_Serialize_WithStatPowerRatio()
        {
            // Arrange
            var skill = SkillFactory.Get(_skillRow, 100, 50, 25, StatType.ATK);

            // Act
            var serialized = skill.Serialize();

            // Assert
            Assert.IsType<List>(serialized);
            var list = (List)serialized;
            Assert.Equal(5, list.Count);

            // First element should be the skill row (also a List)
            Assert.IsType<List>(list[0]);
            var skillRowList = (List)list[0];
            Assert.Equal(8, skillRowList.Count);

            Assert.Equal(100, list[1].ToInteger());
            Assert.Equal(50, list[2].ToInteger());
            Assert.Equal(25, list[3].ToInteger());
            Assert.Equal(StatType.ATK.Serialize(), list[4]);
        }

        [Fact]
        public void Skill_Deserialize_FromList()
        {
            // Arrange
            var originalSkill = SkillFactory.Get(_skillRow, 100, 50, 25, StatType.ATK);
            var serialized = originalSkill.Serialize();

            // Act
            var deserialized = SkillFactory.DeserializeFromList((List)serialized);

            // Assert
            Assert.Equal(originalSkill.SkillRow.Id, deserialized.SkillRow.Id);
            Assert.Equal(originalSkill.Power, deserialized.Power);
            Assert.Equal(originalSkill.Chance, deserialized.Chance);
            Assert.Equal(originalSkill.StatPowerRatio, deserialized.StatPowerRatio);
            Assert.Equal(originalSkill.ReferencedStatType, deserialized.ReferencedStatType);
        }

        [Fact]
        public void Skill_Deserialize_FromDictionary_BackwardCompatibility()
        {
            // Arrange
            var originalSkill = SkillFactory.Get(_skillRow, 100, 50, 25, StatType.ATK);
            var dict = new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"skillRow"] = LegacySerializeSkillRow(originalSkill.SkillRow),
                [(Text)"power"] = originalSkill.Power.Serialize(),
                [(Text)"chance"] = originalSkill.Chance.Serialize(),
                [(Text)"stat_power_ratio"] = originalSkill.StatPowerRatio.Serialize(),
                [(Text)"referenced_stat_type"] = originalSkill.ReferencedStatType.Serialize(),
            });

            // Act
            var deserialized = SkillFactory.DeserializeFromDictionary(dict);

            // Assert
            Assert.Equal(originalSkill.SkillRow.Id, deserialized.SkillRow.Id);
            Assert.Equal(originalSkill.Power, deserialized.Power);
            Assert.Equal(originalSkill.Chance, deserialized.Chance);
            Assert.Equal(originalSkill.StatPowerRatio, deserialized.StatPowerRatio);
            Assert.Equal(originalSkill.ReferencedStatType, deserialized.ReferencedStatType);
        }

        [Fact]
        public void Skill_Serialize_Deserialize_RoundTrip()
        {
            // Arrange
            var originalSkill = SkillFactory.Get(_skillRow, 100, 50, 25, StatType.ATK);

            // Act
            var serialized = originalSkill.Serialize();
            var deserialized = SkillFactory.DeserializeFromList((List)serialized);

            // Assert
            Assert.Equal(originalSkill.SkillRow.Id, deserialized.SkillRow.Id);
            Assert.Equal(originalSkill.SkillRow.ElementalType, deserialized.SkillRow.ElementalType);
            Assert.Equal(originalSkill.SkillRow.SkillType, deserialized.SkillRow.SkillType);
            Assert.Equal(originalSkill.SkillRow.SkillCategory, deserialized.SkillRow.SkillCategory);
            Assert.Equal(originalSkill.SkillRow.SkillTargetType, deserialized.SkillRow.SkillTargetType);
            Assert.Equal(originalSkill.SkillRow.HitCount, deserialized.SkillRow.HitCount);
            Assert.Equal(originalSkill.SkillRow.Cooldown, deserialized.SkillRow.Cooldown);
            Assert.Equal(originalSkill.SkillRow.Combo, deserialized.SkillRow.Combo);
            Assert.Equal(originalSkill.Power, deserialized.Power);
            Assert.Equal(originalSkill.Chance, deserialized.Chance);
            Assert.Equal(originalSkill.StatPowerRatio, deserialized.StatPowerRatio);
            Assert.Equal(originalSkill.ReferencedStatType, deserialized.ReferencedStatType);
        }

        [Theory]
        [InlineData(ElementalType.Normal)]
        [InlineData(ElementalType.Fire)]
        [InlineData(ElementalType.Water)]
        [InlineData(ElementalType.Land)]
        [InlineData(ElementalType.Wind)]
        public void SkillSheet_Row_Enum_ElementalType_Consistency(ElementalType elementalType)
        {
            // Arrange
            var skillDict = new Dictionary<IKey, IValue>
            {
                [(Text)"id"] = (Integer)1,
                [(Text)"elemental_type"] = (Text)elementalType.ToString(),
                [(Text)"skill_type"] = (Text)"Attack",
                [(Text)"skill_category"] = (Text)"NormalAttack",
                [(Text)"skill_target_type"] = (Text)"Enemy",
                [(Text)"hit_count"] = (Integer)1,
                [(Text)"cooldown"] = (Integer)0,
                [(Text)"combo"] = (Bencodex.Types.Boolean)false,
            };
            var dict = new Bencodex.Types.Dictionary(skillDict);

            // Act - Deserialize from Dictionary (legacy format)
            var deserializedFromDict = SkillSheet.Row.Deserialize(dict);

            // Serialize to List (new format)
            var serializedToList = deserializedFromDict.Serialize();

            // Deserialize from List (new format)
            var deserializedFromList = SkillSheet.Row.Deserialize(serializedToList);

            // Assert - All enum values should be consistent
            Assert.Equal(elementalType, deserializedFromDict.ElementalType);
            Assert.Equal(elementalType, deserializedFromList.ElementalType);
            Assert.Equal(deserializedFromDict.ElementalType, deserializedFromList.ElementalType);
        }

        [Theory]
        [InlineData(SkillType.Attack)]
        [InlineData(SkillType.Heal)]
        [InlineData(SkillType.Buff)]
        [InlineData(SkillType.Debuff)]
        public void SkillSheet_Row_Enum_SkillType_Consistency(SkillType skillType)
        {
            // Arrange
            var skillDict = new Dictionary<IKey, IValue>
            {
                [(Text)"id"] = (Integer)1,
                [(Text)"elemental_type"] = (Text)"Normal",
                [(Text)"skill_type"] = (Text)skillType.ToString(),
                [(Text)"skill_category"] = (Text)"NormalAttack",
                [(Text)"skill_target_type"] = (Text)"Enemy",
                [(Text)"hit_count"] = (Integer)1,
                [(Text)"cooldown"] = (Integer)0,
                [(Text)"combo"] = (Bencodex.Types.Boolean)false,
            };
            var dict = new Bencodex.Types.Dictionary(skillDict);

            // Act - Deserialize from Dictionary (legacy format)
            var deserializedFromDict = SkillSheet.Row.Deserialize(dict);

            // Serialize to List (new format)
            var serializedToList = deserializedFromDict.Serialize();

            // Deserialize from List (new format)
            var deserializedFromList = SkillSheet.Row.Deserialize(serializedToList);

            // Assert - All enum values should be consistent
            Assert.Equal(skillType, deserializedFromDict.SkillType);
            Assert.Equal(skillType, deserializedFromList.SkillType);
            Assert.Equal(deserializedFromDict.SkillType, deserializedFromList.SkillType);
        }

        [Theory]
        [InlineData(SkillCategory.NormalAttack)]
        [InlineData(SkillCategory.DoubleAttack)]
        [InlineData(SkillCategory.BlowAttack)]
        [InlineData(SkillCategory.AreaAttack)]
        [InlineData(SkillCategory.BuffRemovalAttack)]
        [InlineData(SkillCategory.ShatterStrike)]
        public void SkillSheet_Row_Enum_SkillCategory_Consistency(SkillCategory skillCategory)
        {
            // Arrange
            var skillDict = new Dictionary<IKey, IValue>
            {
                [(Text)"id"] = (Integer)1,
                [(Text)"elemental_type"] = (Text)"Normal",
                [(Text)"skill_type"] = (Text)"Attack",
                [(Text)"skill_category"] = (Text)skillCategory.ToString(),
                [(Text)"skill_target_type"] = (Text)"Enemy",
                [(Text)"hit_count"] = (Integer)1,
                [(Text)"cooldown"] = (Integer)0,
                [(Text)"combo"] = (Bencodex.Types.Boolean)false,
            };
            var dict = new Bencodex.Types.Dictionary(skillDict);

            // Act - Deserialize from Dictionary (legacy format)
            var deserializedFromDict = SkillSheet.Row.Deserialize(dict);

            // Serialize to List (new format)
            var serializedToList = deserializedFromDict.Serialize();

            // Deserialize from List (new format)
            var deserializedFromList = SkillSheet.Row.Deserialize(serializedToList);

            // Assert - All enum values should be consistent
            Assert.Equal(skillCategory, deserializedFromDict.SkillCategory);
            Assert.Equal(skillCategory, deserializedFromList.SkillCategory);
            Assert.Equal(deserializedFromDict.SkillCategory, deserializedFromList.SkillCategory);
        }

        [Theory]
        [InlineData(SkillTargetType.Enemy)]
        [InlineData(SkillTargetType.Ally)]
        [InlineData(SkillTargetType.Self)]
        public void SkillSheet_Row_Enum_SkillTargetType_Consistency(SkillTargetType skillTargetType)
        {
            // Arrange
            var skillDict = new Dictionary<IKey, IValue>
            {
                [(Text)"id"] = (Integer)1,
                [(Text)"elemental_type"] = (Text)"Normal",
                [(Text)"skill_type"] = (Text)"Attack",
                [(Text)"skill_category"] = (Text)"NormalAttack",
                [(Text)"skill_target_type"] = (Text)skillTargetType.ToString(),
                [(Text)"hit_count"] = (Integer)1,
                [(Text)"cooldown"] = (Integer)0,
                [(Text)"combo"] = (Bencodex.Types.Boolean)false,
            };
            var dict = new Bencodex.Types.Dictionary(skillDict);

            // Act - Deserialize from Dictionary (legacy format)
            var deserializedFromDict = SkillSheet.Row.Deserialize(dict);

            // Serialize to List (new format)
            var serializedToList = deserializedFromDict.Serialize();

            // Deserialize from List (new format)
            var deserializedFromList = SkillSheet.Row.Deserialize(serializedToList);

            // Assert - All enum values should be consistent
            Assert.Equal(skillTargetType, deserializedFromDict.SkillTargetType);
            Assert.Equal(skillTargetType, deserializedFromList.SkillTargetType);
            Assert.Equal(deserializedFromDict.SkillTargetType, deserializedFromList.SkillTargetType);
        }

        [Fact]
        public void SkillSheet_Row_Enum_Format_Conversion_Consistency()
        {
            // Arrange - Create skill row with all enum values
            var skillDict = new Dictionary<IKey, IValue>
            {
                [(Text)"id"] = (Integer)999,
                [(Text)"elemental_type"] = (Text)"Fire",
                [(Text)"skill_type"] = (Text)"Buff",
                [(Text)"skill_category"] = (Text)"AreaAttack",
                [(Text)"skill_target_type"] = (Text)"Ally",
                [(Text)"hit_count"] = (Integer)3,
                [(Text)"cooldown"] = (Integer)5,
                [(Text)"combo"] = (Bencodex.Types.Boolean)true,
            };
            var dict = new Bencodex.Types.Dictionary(skillDict);

            // Act - Test Dictionary -> List -> Dictionary conversion
            var step1 = SkillSheet.Row.Deserialize(dict);           // Dictionary -> Object
            var step2 = step1.Serialize();                          // Object -> List
            var step3 = SkillSheet.Row.Deserialize(step2);          // List -> Object
            var step4 = step3.Serialize();                          // Object -> List
            var step5 = SkillSheet.Row.Deserialize(step4);          // List -> Object

            // Assert - All conversions should preserve enum values
            Assert.Equal(ElementalType.Fire, step1.ElementalType);
            Assert.Equal(SkillType.Buff, step1.SkillType);
            Assert.Equal(SkillCategory.AreaAttack, step1.SkillCategory);
            Assert.Equal(SkillTargetType.Ally, step1.SkillTargetType);

            Assert.Equal(ElementalType.Fire, step3.ElementalType);
            Assert.Equal(SkillType.Buff, step3.SkillType);
            Assert.Equal(SkillCategory.AreaAttack, step3.SkillCategory);
            Assert.Equal(SkillTargetType.Ally, step3.SkillTargetType);

            Assert.Equal(ElementalType.Fire, step5.ElementalType);
            Assert.Equal(SkillType.Buff, step5.SkillType);
            Assert.Equal(SkillCategory.AreaAttack, step5.SkillCategory);
            Assert.Equal(SkillTargetType.Ally, step5.SkillTargetType);

            // All steps should be identical
            Assert.Equal(step1.ElementalType, step3.ElementalType);
            Assert.Equal(step1.SkillType, step3.SkillType);
            Assert.Equal(step1.SkillCategory, step3.SkillCategory);
            Assert.Equal(step1.SkillTargetType, step3.SkillTargetType);

            Assert.Equal(step3.ElementalType, step5.ElementalType);
            Assert.Equal(step3.SkillType, step5.SkillType);
            Assert.Equal(step3.SkillCategory, step5.SkillCategory);
            Assert.Equal(step3.SkillTargetType, step5.SkillTargetType);
        }

        [Theory]
        [InlineData(StatType.NONE)]
        [InlineData(StatType.HP)]
        [InlineData(StatType.ATK)]
        [InlineData(StatType.DEF)]
        [InlineData(StatType.CRI)]
        [InlineData(StatType.HIT)]
        [InlineData(StatType.SPD)]
        public void Skill_StatType_Enum_Consistency(StatType statType)
        {
            // Arrange
            var skill = SkillFactory.Get(_skillRow, 100, 50, 25, statType);

            // Act - Test List format serialization/deserialization
            var serialized = skill.Serialize();
            var deserialized = SkillFactory.DeserializeFromList((List)serialized);

            // Act - Test Dictionary format serialization/deserialization
            var legacySerialized = LegacySerializeSkill(skill);
            var legacyDeserialized = SkillFactory.DeserializeFromDictionary((Dictionary)legacySerialized);

            // Assert - Both formats should preserve StatType enum
            Assert.Equal(statType, deserialized.ReferencedStatType);
            Assert.Equal(statType, legacyDeserialized.ReferencedStatType);
            Assert.Equal(deserialized.ReferencedStatType, legacyDeserialized.ReferencedStatType);
        }
    }
}
