using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Equipment2 : ItemUsable2, IEquippableItem
    {
        public int level;
        public DecimalStat Stat { get; }
        public int SetId { get; }
        public string SpineResourcePath { get; }
        public StatType UniqueStatType => Stat.Type;
        
        private bool _equipped = false;
        public bool Equipped => _equipped;

        public decimal GetIncrementAmountOfEnhancement()
        {
            return Math.Max(1.0m, StatsMap.GetStat(UniqueStatType, true) * 0.1m);
        }

        public Equipment2(
            int serializedVersion,
            EquipmentItemSheet.Row data,
            Guid id,
            long requiredBlockIndex,
            int requiredCharacterLevel)
            : base(serializedVersion, data, id, requiredBlockIndex, requiredCharacterLevel)
        {
            Stat = data.Stat;
            SetId = data.SetId;
            SpineResourcePath = data.SpineResourcePath;
        }

        public Equipment2(Dictionary serialized) : base(serialized)
        {
            if (serialized.TryGetValue((Text) "equipped", out var toEquipped))
            {
                _equipped = toEquipped.ToBoolean();
            }
            if (serialized.TryGetValue((Text) "level", out var toLevel))
            {
                try
                {
                    level = toLevel.ToInteger();
                }
                catch (InvalidCastException)
                {
                    level = (int) ((Integer) toLevel).Value;
                }
            }
            if (serialized.TryGetValue((Text) "stat", out var stat))
            {
                Stat = stat.ToDecimalStat();
            }
            if (serialized.TryGetValue((Text) "set_id", out var setId))
            {
                SetId = setId.ToInteger();
            }
            if (serialized.TryGetValue((Text) "spine_resource_path", out var spineResourcePath))
            {
                SpineResourcePath = (Text) spineResourcePath;
            }
            
            UpdateBaseOptionAndOtherOptions(
                UniqueStatType,
                StatsMap,
                Skills,
                BuffSkills);
        }

        protected Equipment2(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "equipped"] = _equipped.Serialize(),
                [(Text) "level"] = level.Serialize(),
                [(Text) "stat"] = Stat.Serialize(),
                [(Text) "set_id"] = SetId.Serialize(),
                [(Text) "spine_resource_path"] = SpineResourcePath.Serialize(),
            }.Union((Dictionary) base.Serialize()));

#pragma warning restore LAA1002

        public void Equip()
        {
            _equipped = true;
        }

        public void Unequip()
        {
            _equipped = false;
        }

        // FIXME: 기본 스탯을 복리로 증가시키고 있는데, 단리로 증가시켜야 한다.
        // 이를 위해서는 기본 스탯을 유지하면서 추가 스탯에 더해야 하는데, UI 표현에 문제가 생기기 때문에 논의 후 개선한다.
        // 장비가 보유한 스킬의 확률과 수치 강화가 필요한 상태이다.
        public void LevelUp()
        {
            level++;
            StatsMap.AddStatValue(UniqueStatType, GetIncrementAmountOfEnhancement());
            if (new[] {4, 7, 10}.Contains(level) &&
                GetOptionCount() > 0)
            {
                UpdateOptions();
            }
        }

        public List<object> GetOptions()
        {
            var options = new List<object>();
            options.AddRange(Skills);
            options.AddRange(BuffSkills);
            foreach (var statMapEx in StatsMap.GetAdditionalStats())
            {
                options.Add(new StatModifier(
                    statMapEx.StatType,
                    StatModifier.OperationType.Add,
                    statMapEx.AdditionalValueAsInt));
            }

            return options;
        }

        private void UpdateOptions()
        {
            foreach (var statMapEx in StatsMap.GetAdditionalStats())
            {
                StatsMap.SetStatAdditionalValue(
                    statMapEx.StatType,
                    statMapEx.AdditionalValue * 1.3m);
            }

            var skills = new List<Skill.Skill>();
            skills.AddRange(Skills);
            skills.AddRange(BuffSkills);
            foreach (var skill in skills)
            {
                var chance = decimal.ToInt32(skill.Chance * 1.3m);
                var power = decimal.ToInt32(skill.Power * 1.3m);
                skill.Update(chance, power);
            }
        }

        protected bool Equals(Equipment2 other)
        {
            return base.Equals(other) &&
                   _equipped == other._equipped &&
                   level == other.level &&
                   Equals(Stat, other.Stat) &&
                   SetId == other.SetId &&
                   SpineResourcePath == other.SpineResourcePath;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Equipment2) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ _equipped.GetHashCode();
                hashCode = (hashCode * 397) ^ level;
                hashCode = (hashCode * 397) ^ (Stat != null ? Stat.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ SetId;
                hashCode = (hashCode * 397) ^ (SpineResourcePath != null ? SpineResourcePath.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
