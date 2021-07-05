using System;
using Bencodex.Types;
using Nekoyume.Model.State;
using Serilog;
using BxInteger = Bencodex.Types.Integer;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public enum ItemOptionType
    {
        Stat = 0,
        Skill = 1,
    }

    public static class ItemOptionTypeExtensions
    {
        public static IValue Serialize(this ItemOptionType value) =>
            ((int) value).Serialize();

        public static bool TryParse(this IValue value, out ItemOptionType itemOptionType)
        {
            try
            {
                itemOptionType = (ItemOptionType) value.ToInteger();
                return true;
            }
            catch (InvalidCastException e)
            {
                Log.Debug("{Exception}", e.ToString());
                itemOptionType = default;
                return false;
            }
        }
    }
}
