using System;

namespace Nekoyume.Action.Exceptions.CustomEquipmentCraft
{
    public class RandomOnlyIconException : Exception
    {
        public RandomOnlyIconException(string s) : base(s)
        {
        }

        public RandomOnlyIconException(int iconId)
            : base($"{iconId} only can be made with random selection.")
        {
        }
    }
}
