using System;

namespace Lib9c.DevExtensions.Model
{
    [Serializable]
    public class TestbedCreateAvatar : BaseTestbedModel
    {
        public int Level;
        public int TradableMaterialCount;
        public int MaterialCount;
        public int RuneStoneCount;
        public int SoulStoneCount;
        public bool AddPet;
        public int FoodCount;
        public int[] FullOptionEquipmentRecipeIds;
        public CustomEquipmentItem[] CustomEquipmentItems;
    }

    [Serializable]
    public class CustomEquipmentItem
    {
        public int Id;
        public int Level;
        public int[] OptionIds;
    }
}
