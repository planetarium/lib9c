using System;
using System.Linq;
using Bencodex.Types;

namespace Nekoyume.Exceptions
{
    public static class BencodexTypesExtensions
    {
        /// <summary>
        /// Returns a new list with the value inserted at the specified index.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static List InsertTo(this List list, int index, IValue value)
        {
            if (list.Count <= index)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"Index({index}) is out of range({list.Count}).");
            }

            var newList = list.ToArray();
            newList[index] = value;
            return new List(newList);
        }
    }
}
