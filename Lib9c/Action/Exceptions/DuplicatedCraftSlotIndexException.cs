using System;

namespace Nekoyume.Action.Exceptions
{
    [Serializable]
    public class DuplicatedCraftSlotIndexException : Exception
    {
        public DuplicatedCraftSlotIndexException(string message) : base(message)
        {
        }
    }
}
