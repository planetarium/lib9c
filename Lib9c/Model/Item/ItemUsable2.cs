using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using BxDictionary = Bencodex.Types.Dictionary;
using BxList = Bencodex.Types.List;
using BxNull = Bencodex.Types.Null;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public abstract class ItemUsable2 : ItemBase, INonFungibleItem
    {
        public const string SerializedVersionKey = "v";
        private int _serializedVersion = 1;
        public int SerializedVersion => _serializedVersion;

        public const string BaseStatOptionKey = "o";
        private StatOption _baseStatOption;
        public StatOption BaseStatOption => _baseStatOption;

        public const string StatOptionsKey = "o1";
        private List<StatOption> _statOptions = new List<StatOption>();
        public IReadOnlyList<StatOption> StatOptions => _statOptions;

        public const string SkillOptionsKey = "o2";
        private List<SkillOption> _skillOptions = new List<SkillOption>();
        public IReadOnlyList<SkillOption> SkillOptions => _skillOptions;

        public const string RequiredBlockIndexKey = "r";
        private long _requiredBlockIndex;

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

        public const string RequiredCharacterLevelKey = "r1";
        public readonly int RequiredCharacterLevel;

        public const string ItemIdKey = "i";
        public Guid ItemId { get; }

        public Guid TradableId => ItemId;

        public Guid NonFungibleId => ItemId;

        public StatsMap StatsMap { get; } = new StatsMap();

        public List<Skill.Skill> Skills { get; } = new List<Skill.Skill>();


        /// <summary>
        /// NOTE: BuffSkills should be empty. Ref: CombinationEquipmentX.SelectOption()
        /// </summary>
        public List<BuffSkill> BuffSkills { get; } = new List<BuffSkill>();

        protected ItemUsable2(
            int serializedVersion,
            ItemSheet.Row data,
            Guid id,
            long requiredBlockIndex,
            int requiredCharacterLevel)
            : base(data)
        {
            switch (data)
            {
                case ConsumableItemSheet.Row consumableItemRow:
                {
                    foreach (var statData in consumableItemRow.Stats)
                    {
                        if (_baseStatOption is null)
                        {
                            // NOTE: 소모품 옵션의 등급은 1로 고정합니다.
                            _baseStatOption = new StatOption(1, statData);
                        }
                        else
                        {
                            // NOTE: 소모품 옵션의 등급은 1로 고정합니다.
                            _statOptions.Add(new StatOption(1, statData));
                        }
                    }

                    _statOptions = _statOptions
                        .OrderByDescending(e => e.Grade)
                        .ToList();

                    break;
                }
                case EquipmentItemSheet.Row equipmentItemRow:
                    // NOTE: 주 옵션의 등급은 1로 고정합니다.
                    _baseStatOption = new StatOption(1, equipmentItemRow.Stat);
                    break;
            }

            _serializedVersion = serializedVersion;
            ItemId = id;
            UpdateStatsMap();
            UpdateSkills();
            RequiredBlockIndex = requiredBlockIndex;
            RequiredCharacterLevel = requiredCharacterLevel;
        }

        protected ItemUsable2(BxDictionary serialized) : base((Dictionary) serialized[BaseKey])
        {
            if (serialized.ContainsKey(SerializedVersionKey))
            {
                _serializedVersion = serialized[SerializedVersionKey].ToInteger();

                ItemId = serialized[ItemIdKey].ToGuid();

                switch (_serializedVersion)
                {
                    case 2:
                        Deserialize2(serialized);
                        break;
                    default:
                        throw new DeserializeFailedException(
                            $"Deserialize for version({_serializedVersion}) is not implemented");
                }
            }
            else
            {
                if (serialized.TryGetValue((Text) LegacyItemIdKey, out var itemId))
                {
                    ItemId = itemId.ToGuid();
                }

                Deserialize1(serialized);
            }
        }

        protected ItemUsable2(SerializationInfo info, StreamingContext _)
            : this((BxDictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        protected bool Equals(ItemUsable2 other)
        {
            return base.Equals(other) && Equals(ItemId, other.ItemId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ItemUsable2) obj);
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
            return StatsMap.GetAdditionalStats().Count()
                   + Skills.Count
                   + BuffSkills.Count;
        }

        public void Update(long blockIndex)
        {
            RequiredBlockIndex = blockIndex;
        }

        public override IValue Serialize()
        {
            switch (_serializedVersion)
            {
                case 1:
                    return Serialize1();
                case 2:
                    return Serialize2();
                default:
                    throw new SerializeFailedException($"Serialize{_serializedVersion}() is not implemented");
            }
        }

        /// <param name="serializeVersion"></param>
        public IValue SerializeAs(int serializeVersion)
        {
            if (serializeVersion == _serializedVersion)
            {
                return Serialize();
            }

            _serializedVersion = serializeVersion;
            return Serialize();
        }

        private IValue Serialize1() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) LegacyItemIdKey] = ItemId.Serialize(),
                [(Text) LegacyStatsMapKey] = StatsMap.Serialize(),
                [(Text) LegacySkillsKey] = new List(Skills
                    .OrderByDescending(i => i.Chance)
                    .ThenByDescending(i => i.Power)
                    .Select(s => s.Serialize())),
                [(Text) LegacyBuffSkillsKey] = new List(BuffSkills
                    .OrderByDescending(i => i.Chance)
                    .ThenByDescending(i => i.Power)
                    .Select(s => s.Serialize())),
                [(Text) LegacyRequiredBlockIndexKey] = RequiredBlockIndex.Serialize(),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002

        private IValue Serialize2() => BxDictionary.Empty
            .SetItem(BaseKey, base.Serialize())
            .SetItem(SerializedVersionKey, _serializedVersion.Serialize())
            .SetItem(ItemIdKey, ItemId.Serialize())
            .SetItem(BaseStatOptionKey, _baseStatOption?.Serialize() ?? BxNull.Value)
            .SetItem(StatOptionsKey, (IValue) new BxList(_statOptions.Select(e => e.Serialize())))
            .SetItem(SkillOptionsKey, (IValue) new BxList(_skillOptions.Select(e => e.Serialize())))
            .SetItem(RequiredBlockIndexKey, RequiredBlockIndex.Serialize())
            .SetItem(RequiredCharacterLevelKey, RequiredCharacterLevel.Serialize());

        private void Deserialize1(BxDictionary serialized)
        {
            if (serialized.TryGetValue((Text) LegacyRequiredBlockIndexKey, out var requiredBlockIndex))
            {
                RequiredBlockIndex = requiredBlockIndex.ToLong();
            }

            if (serialized.TryGetValue((Text) LegacyStatsMapKey, out var statsMap))
            {
                StatsMap.Deserialize((BxDictionary) statsMap);
            }

            if (serialized.TryGetValue((Text) LegacySkillsKey, out var skills))
            {
                foreach (var value in (List) skills)
                {
                    var skill = (BxDictionary) value;
                    Skills.Add(SkillFactory.Deserialize(skill));
                }
            }

            if (serialized.TryGetValue((Text) LegacyBuffSkillsKey, out var buffSkills))
            {
                foreach (var value in (List) buffSkills)
                {
                    var buffSkill = (BxDictionary) value;
                    BuffSkills.Add((BuffSkill) SkillFactory.Deserialize(buffSkill));
                }
            }
        }

        private void Deserialize2(BxDictionary serialized)
        {
            _baseStatOption = StatOption.TryDeserialize(serialized[BaseStatOptionKey], out var baseStatOption)
                ? baseStatOption
                : null;
            _statOptions = ((BxList) serialized[StatOptionsKey])
                .Select(e => new StatOption(e))
                .OrderByDescending(e => e.Grade)
                .ToList();
            _skillOptions = ((BxList) serialized[SkillOptionsKey])
                .Select(e => new SkillOption(e))
                .OrderByDescending(e => e.Grade)
                .ToList();
            _requiredBlockIndex = serialized[RequiredBlockIndexKey].ToLong();

            UpdateStatsMap();
            UpdateSkills();
        }

        public void AddStatOption(StatOption statOption)
        {
            _statOptions = _statOptions
                .Append(statOption)
                .OrderByDescending(e => e.Grade)
                .ToList();
            StatsMap.AddStatAdditionalValue(statOption);
        }

        public void AddStatOption(int grade, StatType statType, decimal statValue) =>
            AddStatOption(new StatOption(grade, statType, statValue));

        public void AddSkillOption(SkillOption skillOption)
        {
            _skillOptions = _skillOptions
                .Append(skillOption)
                .OrderByDescending(e => e.Grade)
                .ToList();
            // NOTE: BuffSkills should be empty. Ref: CombinationEquipmentX.SelectOption()
            Skills.Add(skillOption.Skill);
        }

        public void AddSkillOption(int grade, Skill.Skill skill) =>
            AddSkillOption(new SkillOption(grade, skill));

        protected void UpdateBaseOptionAndOtherOptions(
            StatType baseStatType = default,
            StatsMap statsMap = default,
            List<Skill.Skill> skills = default,
            List<Skill.BuffSkill> buffSkills = default)
        {
            if (!(statsMap is null))
            {
                // StatsMap to `_baseStatOption` and `_statOptions`
                var statMaps = statsMap.GetStats();
                foreach (var statMapEx in statMaps)
                {
                    if (statMapEx.StatType == baseStatType)
                    {
                        // NOTE: Set option grade to `1` because `StatsMap` has no option level.
                        _baseStatOption = new StatOption(
                            1,
                            statMapEx.StatType,
                            statMapEx.Value);

                        if (statMapEx.AdditionalValue == 0)
                        {
                            continue;
                        }

                        // NOTE: Set option grade to `1` because `StatsMap` has no option level.
                        _statOptions.Add(new StatOption(
                            1,
                            statMapEx.StatType,
                            statMapEx.AdditionalValue));
                    }
                    else
                    {
                        // NOTE: Set option grade to `1` because `StatsMap` has no option level.
                        _statOptions.Add(new StatOption(
                            1,
                            statMapEx.StatType,
                            statMapEx.Value));
                    }
                }
            }

            // Skills to `_skillOptions`
            if (!(skills is null))
            {
                foreach (var skill in skills)
                {
                    _skillOptions.Add(new SkillOption(1, skill));
                }
            }

            // BuffSkills to `_skillOptions`
            if (!(buffSkills is null))
            {
                foreach (var buffSkill in buffSkills)
                {
                    _skillOptions.Add(new SkillOption(1, buffSkill));
                }
            }
        }

        private void UpdateStatsMap()
        {
            StatsMap.Clear();

            var options = _statOptions.ToArray();
            if (_baseStatOption is null)
            {
                for (var i = options.Length; i > 0; i--)
                {
                    var option = options[i - 1];
                    StatsMap.AddStatAdditionalValue(option);
                }

                return;
            }

            StatsMap.AddStatValue(_baseStatOption.StatType, _baseStatOption.statValue);

            for (var i = options.Length; i > 0; i--)
            {
                var option = options[i - 1];
                if (option.StatType == _baseStatOption.StatType)
                {
                    StatsMap.AddStatValue(option);
                }
                else
                {
                    StatsMap.AddStatAdditionalValue(option);
                }
            }
        }

        private void UpdateSkills()
        {
            Skills.Clear();
            BuffSkills.Clear();

            var options = _skillOptions.ToArray();
            for (var i = options.Length; i > 0; i--)
            {
                var option = options[i - 1];
                // NOTE: BuffSkills should be empty. Ref: CombinationEquipmentX.SelectOption()
                Skills.Add(option.Skill);
            }
        }
    }
}
