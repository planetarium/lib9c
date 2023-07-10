using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;

namespace Nekoyume.Action
{
    public static class StaticSheets
    {
        public static WorldSheet WorldSheet { get; set; }

        public static StageWaveSheet StageWaveSheet { get; set; }

        public static StageSheet StageSheet { get; set; }

        public static CharacterSheet CharacterSheet { get; set; }

        public static CharacterLevelSheet CharacterLevelSheet { get; set; }

        public static SkillSheet SkillSheet { get; set; }

        public static StatBuffSheet StatBuffSheet { get; set; }

        public static ItemSheet ItemSheet { get; set; }

        public static ItemRequirementSheet ItemRequirementSheet { get; set; }

        public static ConsumableItemSheet ConsumableItemSheet { get; set; }

        public static CostumeItemSheet CostumeItemSheet { get; set; }

        public static EquipmentItemSheet EquipmentItemSheet { get; set; }

        public static MaterialItemSheet MaterialItemSheet { get; set; }

        public static QuestSheet QuestSheet { get; set; }

        public static WorldQuestSheet WorldQuestSheet { get; set; }

        public static CollectQuestSheet CollectQuestSheet { get; set; }

        public static CombinationQuestSheet CombinationQuestSheet { get; set; }

        public static TradeQuestSheet TradeQuestSheet { get; set; }

        public static ItemEnhancementQuestSheet ItemEnhancementQuestSheet { get; set; }

        public static GeneralQuestSheet GeneralQuestSheet { get; set; }

        public static SkillBuffSheet SkillBuffSheet { get; set; }

        public static MonsterQuestSheet MonsterQuestSheet { get; set; }

        public static ItemGradeQuestSheet ItemGradeQuestSheet { get; set; }

        public static ItemTypeCollectQuestSheet ItemTypeCollectQuestSheet { get; set; }

        public static GoldQuestSheet GoldQuestSheet { get; set; }

        public static EquipmentItemSetEffectSheet EquipmentItemSetEffectSheet { get; set; }

        public static EnemySkillSheet EnemySkillSheet { get; set; }

        public static QuestRewardSheet QuestRewardSheet { get; set; }

        public static QuestItemRewardSheet QuestItemRewardSheet { get; set; }

        public static WorldUnlockSheet WorldUnlockSheet { get; set; }

        public static EquipmentItemRecipeSheet EquipmentItemRecipeSheet { get; set; }

        public static EquipmentItemSubRecipeSheetV2 EquipmentItemSubRecipeSheetV2 { get; set; }

        public static EquipmentItemOptionSheet EquipmentItemOptionSheet { get; set; }

        public static CombinationEquipmentQuestSheet CombinationEquipmentQuestSheet { get; set; }

        public static CostumeStatSheet CostumeStatSheet { get; set; }

        public static CrystalStageBuffGachaSheet CrystalStageBuffGachaSheet { get; set; }

        public static CrystalRandomBuffSheet CrystalRandomBuffSheet { get; set; }


        public static StakeActionPointCoefficientSheet StakeActionPointCoefficientSheet
        {
            get;
            set;
        }

        public static SkillActionBuffSheet SkillActionBuffSheet { get; set; }

        public static ActionBuffSheet ActionBuffSheet { get; set; }

        public static RuneListSheet RuneListSheet { get; set; }

        public static RuneOptionSheet RuneOptionSheet { get; set; }

        public static void ItemSheetInitialize()
        {
            ItemSheet ??= new ItemSheet();
            ItemSheet.Set(ConsumableItemSheet, false);
            ItemSheet.Set(CostumeItemSheet, false);
            ItemSheet.Set(EquipmentItemSheet, false);
            ItemSheet.Set(MaterialItemSheet);
        }

        public static void QuestSheetInitialize()
        {
            QuestSheet ??= new QuestSheet();
            QuestSheet.Set(WorldQuestSheet, false);
            QuestSheet.Set(CollectQuestSheet, false);
            QuestSheet.Set(CombinationQuestSheet, false);
            QuestSheet.Set(TradeQuestSheet, false);
            QuestSheet.Set(MonsterQuestSheet, false);
            QuestSheet.Set(ItemEnhancementQuestSheet, false);
            QuestSheet.Set(GeneralQuestSheet, false);
            QuestSheet.Set(ItemGradeQuestSheet, false);
            QuestSheet.Set(ItemTypeCollectQuestSheet, false);
            QuestSheet.Set(GoldQuestSheet, false);
            QuestSheet.Set(CombinationEquipmentQuestSheet);
        }

        public static SimulatorSheets GetSimulatorSheets()
        {
            return new SimulatorSheets(
                MaterialItemSheet,
                SkillSheet,
                SkillBuffSheet,
                StatBuffSheet,
                SkillActionBuffSheet,
                ActionBuffSheet,
                CharacterSheet,
                CharacterLevelSheet,
                EquipmentItemSetEffectSheet,
                RuneOptionSheet
            );
        }
    }
}
