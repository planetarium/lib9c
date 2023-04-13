using System.Collections.Generic;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.Model.Stat
{
    public interface ICharacterStats: IStats, IBaseAndAdditionalStats
    {
        public CharacterSheet.Row row { get; set; }
        public IStats baseStats { get; }
        public IStats equipmentStats { get; }
        public IStats consumableStats { get; }
        public IStats buffStats { get; }
        public IStats optionalStats { get; }

        public List<StatModifier> equipmentStatModifiers { get; }
        public List<StatModifier> consumableStatModifiers { get; }
        public Dictionary<int, StatModifier> buffStatModifiers { get; }
        public List<StatModifier> optionalStatModifiers { get; }
        public IStats BaseStats { get; }
        public IStats EquipmentStats { get; }
        public IStats BuffStats { get; }
        public IStats OptionalStats { get; }

        public int Level { get; set; }
        public int CurrentHP { get; set; }

        public ICharacterStats SetStats(int level, bool updateImmediate = true);

        public void AddOption(IEnumerable<StatModifier> statModifiers);

        public CharacterStats SetBuffs(IEnumerable<Buff.StatBuff> value,
            bool updateImmediate = true);

        public void AddBuff(Buff.StatBuff buff, bool updateImmediate = true);

        public void RemoveBuff(Buff.StatBuff buff, bool updateImmediate = true);

        public ICharacterStats SetEquipments(
            IEnumerable<Equipment> value,
            EquipmentItemSetEffectSheet sheet,
            bool updateImmediate = true
        );

        public CharacterStats SetConsumables(IEnumerable<Consumable> value,
            bool updateImmediate = true);

        public void SetOption(IEnumerable<StatModifier> statModifiers);

        public void EqualizeCurrentHPWithHP();
        void SetStatForTest(StatType p0, int p1);
    }
}
