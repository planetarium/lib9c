using System;
using System.Collections.Generic;
using Bencodex.Types;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;
using Nekoyume.Model.State;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class SkillSheet : Sheet<int, SkillSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>, IState
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public ElementalType ElementalType { get; private set; }
            public SkillType SkillType { get; private set; }
            public SkillCategory SkillCategory { get; private set; }
            public SkillTargetType SkillTargetType { get; private set; }
            public int HitCount { get; private set; }
            public int Cooldown { get; private set; }

            public bool Combo { get; private set; }
            public Row() {}

            public Row(Bencodex.Types.Dictionary serialized)
            {
                Id = (Bencodex.Types.Integer) serialized["id"];
                ElementalType = (ElementalType) Enum.Parse(typeof(ElementalType),
                    (Bencodex.Types.Text) serialized["elemental_type"]);
                SkillType = (SkillType) Enum.Parse(typeof(SkillType), (Bencodex.Types.Text) serialized["skill_type"]);
                SkillCategory = (SkillCategory) Enum.Parse(typeof(SkillCategory), (Bencodex.Types.Text) serialized["skill_category"]);
                SkillTargetType = (SkillTargetType) Enum.Parse(typeof(SkillTargetType), (Bencodex.Types.Text) serialized["skill_target_type"]);
                HitCount = (Bencodex.Types.Integer) serialized["hit_count"];
                Cooldown = (Bencodex.Types.Integer) serialized["cooldown"];
                if (serialized.ContainsKey("combo"))
                {
                    Combo = serialized["combo"] is not null && (Bencodex.Types.Boolean)serialized["combo"];
                }
            }

            public Row(Bencodex.Types.List serialized)
            {
                Id = (Bencodex.Types.Integer) serialized[0];
                ElementalType = (ElementalType) (int)(Integer) serialized[1];
                SkillType = (SkillType) (int)(Integer) serialized[2];
                SkillCategory = (SkillCategory) (int)(Integer) serialized[3];
                SkillTargetType = (SkillTargetType) (int)(Integer) serialized[4];
                HitCount = (Bencodex.Types.Integer) serialized[5];
                Cooldown = (Bencodex.Types.Integer) serialized[6];
                Combo = serialized[7].ToBoolean();
            }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                ElementalType = (ElementalType) Enum.Parse(typeof(ElementalType), fields[1]);
                SkillType = (SkillType) Enum.Parse(typeof(SkillType), fields[2]);
                SkillCategory = (SkillCategory) Enum.Parse(typeof(SkillCategory), fields[3]);
                SkillTargetType = (SkillTargetType) Enum.Parse(typeof(SkillTargetType), fields[4]);
                HitCount = ParseInt(fields[5]);
                Cooldown = ParseInt(fields[6]);
                Combo = fields.Count > 7 && ParseBool(fields[7], false);
            }

            public IValue Serialize()
            {
                var list = new List<IValue>
                {
                    (Integer)Id,
                    (Integer)(int)ElementalType,
                    (Integer)(int)SkillType,
                    (Integer)(int)SkillCategory,
                    (Integer)(int)SkillTargetType,
                    (Integer)HitCount,
                    (Integer)Cooldown,
                    Combo.Serialize(),
                };
                return new List(list);
            }

            /// <summary>
            /// Deserializes a skill row from serialized data.
            /// Supports both Dictionary and List formats for backward compatibility.
            /// </summary>
            /// <param name="serialized">The serialized skill row data</param>
            /// <returns>The deserialized skill row</returns>
            /// <exception cref="ArgumentException">Thrown when the serialization format is not supported</exception>
            public static Row Deserialize(IValue serialized)
            {
                switch (serialized)
                {
                    case Dictionary dict:
                        return DeserializeFromDictionary(dict);
                    case List list:
                        return DeserializeFromList(list);
                    default:
                        throw new ArgumentException($"Unsupported serialization format: {serialized.GetType()}");
                }
            }

            public static Row DeserializeFromList(Bencodex.Types.List serialized)
            {
                return new Row(serialized);
            }

            [Obsolete("Use Deserialize(IValue) instead.")]
            public static Row Deserialize(Bencodex.Types.Dictionary serialized)
            {
                return DeserializeFromDictionary(serialized);
            }

            public static Row DeserializeFromDictionary(Bencodex.Types.Dictionary serialized)
            {
                return new Row(serialized);
            }
        }

        public SkillSheet() : base(nameof(SkillSheet))
        {
        }
    }
}
