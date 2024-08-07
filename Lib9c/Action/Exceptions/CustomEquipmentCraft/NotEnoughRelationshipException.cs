using System;

namespace Nekoyume.Action.Exceptions.CustomEquipmentCraft
{
    [Serializable]
    public class NotEnoughRelationshipException : Exception
    {
        public NotEnoughRelationshipException(string s) : base(s)
        {
        }
    }
}
