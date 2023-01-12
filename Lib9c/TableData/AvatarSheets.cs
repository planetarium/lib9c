using Lib9c.TableData.Item;
using Lib9c.TableData.Quest;
using Lib9c.TableData.WorldAndStage;

namespace Lib9c.TableData
{
    public class AvatarSheets
    {
        public readonly WorldSheet WorldSheet;
        public readonly QuestSheet QuestSheet;
        public readonly QuestRewardSheet QuestRewardSheet;
        public readonly QuestItemRewardSheet QuestItemRewardSheet;
        public readonly EquipmentItemRecipeSheet EquipmentItemRecipeSheet;
        public readonly EquipmentItemSubRecipeSheet EquipmentItemSubRecipeSheet;

        public AvatarSheets(
            WorldSheet worldSheet,
            QuestSheet questSheet,
            QuestRewardSheet questRewardSheet,
            QuestItemRewardSheet questItemRewardSheet,
            EquipmentItemRecipeSheet equipmentItemRecipeSheet,
            EquipmentItemSubRecipeSheet equipmentItemSubRecipeSheet
        )
        {
            WorldSheet = worldSheet;
            QuestSheet = questSheet;
            QuestRewardSheet = questRewardSheet;
            QuestItemRewardSheet = questItemRewardSheet;
            EquipmentItemRecipeSheet = equipmentItemRecipeSheet;
            EquipmentItemSubRecipeSheet = equipmentItemSubRecipeSheet;
        }
    }
}
