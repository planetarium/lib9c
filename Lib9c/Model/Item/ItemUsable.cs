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
    /// <summary>
    /// Base class for usable items (consumables and equipment).
    /// Supports both Dictionary and List serialization formats for backward compatibility.
    /// TODO: The model seems to be equipment-oriented when used together with consumables.
    /// Consider refactoring during item reorganization.
    /// </summary>
    // todo: 소모품과 장비가 함께 쓰기에는 장비 위주의 모델이 된 느낌. 아이템 정리하면서 정리를 흐음..
    [Serializable]
    public abstract class ItemUsable : ItemBase, INonFungibleItem
    {
        public Guid ItemId
        {
            get
            {
                if (_serializedItemId is { })
                {
                    _itemId = _serializedItemId.ToGuid();
                    _serializedItemId = null;
                }

                return _itemId;
            }
        }

        public Guid NonFungibleId => ItemId;

        public StatsMap StatsMap
        {
            get
            {
                _statsMap ??= new StatsMap();
                if (_serializedStatsMap is { })
                {
                    _statsMap.Deserialize(_serializedStatsMap);
                    _serializedStatsMap = null;
                }

                return _statsMap;
            }
        }

        public List<Skill.Skill> Skills
        {
            get
            {
                _skills ??= new List<Skill.Skill>();
                if (_serializedSkills is { })
                {
                    foreach (var value in _serializedSkills)
                    {
                        var serializedSkill = (Dictionary) value;
                        _skills.Add(SkillFactory.Deserialize(serializedSkill));
                    }

                    _serializedSkills = null;
                }

                return _skills;
            }
        }

        public List<BuffSkill> BuffSkills
        {
            get
            {
                _buffSkills ??= new List<BuffSkill>();
                if (_serializedBuffSkills is { })
                {
                    foreach (var value in _serializedBuffSkills)
                    {
                        var serializedSkill = (Dictionary) value;
                        _buffSkills.Add((BuffSkill) SkillFactory.Deserialize(serializedSkill));
                    }

                    _serializedBuffSkills = null;
                }

                return _buffSkills;
            }
        }

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
        private Guid _itemId;
        private StatsMap _statsMap;
        private List<Skill.Skill> _skills;
        private List<BuffSkill> _buffSkills;
        private Binary? _serializedItemId;
        private Dictionary _serializedStatsMap;
        private List _serializedSkills;
        private List _serializedBuffSkills;

        protected ItemUsable(ItemSheet.Row data, Guid id, long requiredBlockIndex) : base(data)
        {
            _itemId = id;
            _statsMap = new StatsMap();

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

            _skills = new List<Model.Skill.Skill>();
            _buffSkills = new List<BuffSkill>();
            RequiredBlockIndex = requiredBlockIndex;
        }

        /// <summary>
        /// Constructor for deserialization that supports both Dictionary and List formats.
        /// </summary>
        /// <param name="serialized">Serialized data in either Dictionary or List format</param>
        protected ItemUsable(IValue serialized) : base(serialized)
        {
            switch (serialized)
            {
                case Dictionary dict:
                    DeserializeFromDictionary(dict);
                    break;
                case List list:
                    DeserializeFromList(list);
                    break;
                default:
                    throw new ArgumentException($"Unsupported serialization format: {serialized.GetType()}");
            }
        }

        /// <summary>
        /// Deserializes data from Dictionary format (legacy support).
        /// </summary>
        /// <param name="dict">Dictionary containing serialized data</param>
        private void DeserializeFromDictionary(Dictionary dict)
        {
            if (dict.TryGetValue((Text) "itemId", out var itemId))
            {
                _serializedItemId = (Binary) itemId;
            }
            if (dict.TryGetValue((Text) "statsMap", out var statsMap))
            {
                _serializedStatsMap = (Dictionary) statsMap;
            }
            if (dict.TryGetValue((Text) "skills", out var skills))
            {
                _serializedSkills = (List) skills;
            }
            if (dict.TryGetValue((Text) "buffSkills", out var buffSkills))
            {
                _serializedBuffSkills = (List) buffSkills;
            }
            if (dict.TryGetValue((Text) "requiredBlockIndex", out var requiredBlockIndex))
            {
                RequiredBlockIndex = requiredBlockIndex.ToLong();
            }
        }

        /// <summary>
        /// Deserializes data from List format (new format).
        /// Order: [baseData..., itemId, statsMap, skills, buffSkills, requiredBlockIndex]
        /// </summary>
        /// <param name="list">List containing serialized data</param>
        private void DeserializeFromList(List list)
        {
            // Always read 11 fields (length check removed)
            // base fields (0~5): version, id, itemType, itemSubType, grade, elementalType
            // ItemUsable fields (6~10): itemId, statsMap, skills, buffSkills, requiredBlockIndex

            _serializedItemId = (Binary) list[6];
            _serializedStatsMap = (Dictionary) list[7];
            _serializedSkills = (List) list[8];
            _serializedBuffSkills = (List) list[9];
            RequiredBlockIndex = list[10].ToLong();
        }

        protected ItemUsable(SerializationInfo info, StreamingContext _)
            : this(Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
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

        /// <summary>
        /// Serializes the item to List format (new format).
        /// Order: [baseData..., itemId, statsMap, skills, buffSkills, requiredBlockIndex]
        /// </summary>
        /// <returns>List containing serialized data</returns>
        public override IValue Serialize() => ((List)base.Serialize())
            .Add(_serializedItemId ?? ItemId.Serialize())
            .Add(_serializedStatsMap ?? StatsMap.Serialize())
            .Add(_serializedSkills ?? new List(Skills
                .OrderByDescending(i => i.Chance)
                .ThenByDescending(i => i.Power)
                .Select(s => s.Serialize())))
            .Add(_serializedBuffSkills ?? new List(BuffSkills
                .OrderByDescending(i => i.Chance)
                .ThenByDescending(i => i.Power)
                .Select(s => s.Serialize())))
            .Add(RequiredBlockIndex.Serialize());
    }
}
