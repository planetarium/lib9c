using System.Numerics;

namespace Lib9c.Helper
{
    /// <summary>
    /// 숫자 변환 관련 유틸리티 함수 모음
    /// </summary>
    public static class NumberConversionHelper
    {
        /// <summary>
        /// Safely converts decimal to int32, clamping to int32 bounds to prevent overflow.
        /// </summary>
        /// <param name="value">The decimal value to convert.</param>
        /// <returns>The converted int32 value, clamped to int32 bounds.</returns>
        public static int SafeDecimalToInt32(decimal value)
        {
            return value switch
            {
                > int.MaxValue => int.MaxValue,
                < int.MinValue => int.MinValue,
                _ => (int) value,
            };
        }


        /// <summary>
        /// Safely converts decimal to long, clamping to long bounds to prevent overflow.
        /// </summary>
        /// <param name="value">The decimal value to convert.</param>
        /// <returns>The converted long value, clamped to long bounds.</returns>
        public static long SafeDecimalToInt64(decimal value)
        {
            return value switch
            {
                > long.MaxValue => long.MaxValue,
                < long.MinValue => long.MinValue,
                _ => (long) value,
            };
        }

        /// <summary>
        /// Safely converts BigInteger to int32, clamping to int32 bounds to prevent overflow.
        /// </summary>
        /// <param name="value">The BigInteger value to convert.</param>
        /// <returns>The converted int32 value, clamped to int32 bounds.</returns>
        public static int SafeBigIntegerToInt32(BigInteger value)
        {
            if (value > int.MaxValue)
            {
                return int.MaxValue;
            }

            if (value < int.MinValue)
            {
                return int.MinValue;
            }

            return (int)value;
        }
    }
}
