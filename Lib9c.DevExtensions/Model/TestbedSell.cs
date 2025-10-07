using System;
using Lib9c.Model.Item;

namespace Lib9c.DevExtensions.Model
{
    [Serializable]
    public class TestbedSell : BaseTestbedModel
    {
        public Avatar Avatar;
        public Item[] Items;
    }

    [Serializable]
    public class Avatar
    {
        public string Name;

        protected bool Equals(Avatar other)
        {
            return Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((Avatar)obj);
        }

        public override int GetHashCode()
        {
            return Name != null ? Name.GetHashCode() : 0;
        }
    }

    [Serializable]
    public class Item
    {
        public ItemSubType ItemSubType;
        public int Id;
        public int Level;
        public int Count;
        public int Price;
        public int[] OptionIds;
    }
}
