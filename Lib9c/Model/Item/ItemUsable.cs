using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    // todo: 소모품과 장비가 함께 쓰기에는 장비 위주의 모델이 된 느낌. 아이템 정리하면서 정리를 흐음..
    [Serializable]
    public abstract class ItemUsable : ItemBase, INonFungibleItem
    {
        public Guid ItemId { get; }
        public Guid TradableId => ItemId;
        public Guid NonFungibleId => ItemId;
        public StatsMap StatsMap { get; }
        public List<Skill.Skill> Skills { get; }
        public List<BuffSkill> BuffSkills { get; }

        public long RequiredBlockIndex
        {
            get => _requiredBlockIndex;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        $"{nameof(RequiredBlockIndex)} must be greater than 0, but {value}");
                }
                _requiredBlockIndex = value;
            }
        }

        private long _requiredBlockIndex;

        protected ItemUsable(ItemSheet.Row data, Guid id, long requiredBlockIndex) : base(data)
        {
            ItemId = id;
            StatsMap = new StatsMap();

            switch (data)
            {
                case ConsumableItemSheet.Row consumableItemRow:
                {
                    foreach (var statData in consumableItemRow.Stats)
                    {
                        StatsMap.AddStatValue(statData.StatType, statData.BaseValue);
                    }

                    break;
                }
                case EquipmentItemSheet.Row equipmentItemRow:
                    StatsMap.AddStatValue(equipmentItemRow.Stat.StatType, equipmentItemRow.Stat.BaseValue);
                    break;
            }

            Skills = new List<Model.Skill.Skill>();
            BuffSkills = new List<BuffSkill>();
            RequiredBlockIndex = requiredBlockIndex;
        }

        protected ItemUsable(Dictionary serialized) : base(serialized)
        {
            StatsMap = new StatsMap();
            Skills = new List<Model.Skill.Skill>();
            BuffSkills = new List<BuffSkill>();
            if (serialized.TryGetValue((Text) "itemId", out var itemId))
            {
                ItemId = itemId.ToGuid();
            }
            if (serialized.TryGetValue((Text) "statsMap", out var statsMap))
            {
                StatsMap.Deserialize((Dictionary) statsMap);
            }
            if (serialized.TryGetValue((Text) "skills", out var skills))
            {
                foreach (var value in (List) skills)
                {
                    var skill = (Dictionary) value;
                    Skills.Add(SkillFactory.Deserialize(skill));
                }
            }
            if (serialized.TryGetValue((Text) "buffSkills", out var buffSkills))
            {
                foreach (var value in (List) buffSkills)
                {
                    var buffSkill = (Dictionary) value;
                    BuffSkills.Add((BuffSkill) SkillFactory.Deserialize(buffSkill));
                }
            }
            if (serialized.TryGetValue((Text) "requiredBlockIndex", out var requiredBlockIndex))
            {
                RequiredBlockIndex = requiredBlockIndex.ToLong();
            }
        }

        protected ItemUsable(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        protected bool Equals(ItemUsable other)
        {
            return base.Equals(other) && Equals(ItemId, other.ItemId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ItemUsable) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ ItemId.GetHashCode();
            }
        }

        public int GetOptionCount()
        {
            return StatsMap.GetAdditionalStats(true).Count()
                   + Skills.Count
                   + BuffSkills.Count;
        }

        public void Update(long blockIndex)
        {
            RequiredBlockIndex = blockIndex;
        }

        public override IValue Serialize() => ((Dictionary)base.Serialize())
            .Add("itemId", ItemId.Serialize())
            .Add("statsMap", StatsMap.Serialize())
            .Add("skills", new List(Skills
                .OrderByDescending(i => i.Chance)
                .ThenByDescending(i => i.Power)
                .Select(s => s.Serialize())))
            .Add("buffSkills", new List(BuffSkills
                .OrderByDescending(i => i.Chance)
                .ThenByDescending(i => i.Power)
                .Select(s => s.Serialize())))
            .Add("requiredBlockIndex", RequiredBlockIndex.Serialize());
    }
}
