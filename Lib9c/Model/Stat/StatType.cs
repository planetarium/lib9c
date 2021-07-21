using System;
using System.Collections.Generic;
using Bencodex.Types;

namespace Nekoyume.Model.Stat
{
    public enum StatType
    {
        /// <summary>
        /// Default, It's same with null.
        /// </summary>
        NONE,
        
        /// <summary>
        /// Health Point
        /// </summary>
        HP,

        /// <summary>
        /// Attack Power
        /// </summary>
        ATK,

        /// <summary>
        /// Defence
        /// </summary>
        DEF,

        /// <summary>
        /// Critical Chance
        /// </summary>
        CRI,
        
        /// <summary>
        /// Hit Chance
        /// </summary>
        HIT,
        
        /// <summary>
        /// Speed
        /// </summary>
        SPD
    }

    [Serializable]
    public class StatTypeComparer : IComparer<StatType>, IEqualityComparer<StatType>
    {
        public static readonly StatTypeComparer Instance = new StatTypeComparer();

        public int Compare(StatType x, StatType y)
        {
            return (int) x - (int) y;
        }
        
        public bool Equals(StatType x, StatType y)
        {
            return x == y;
        }

        public int GetHashCode(StatType obj)
        {
            return (int) obj;
        }
    }

    public static class StatTypeExtension
    {
        public static IKey Serialize(this StatType statType) =>
            new Binary(BitConverter.GetBytes((int) statType));

        public static StatType Deserialize(Binary serialized) =>
            (StatType) BitConverter.ToInt32(serialized.ToByteArray(), 0);
    }
}
