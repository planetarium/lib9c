using System;
using Bencodex.Types;

namespace Nekoyume.VersionedStates
{
    /// <summary>
    /// Interface for versioned state.
    /// </summary>
    public interface IVersionedState
    {
        /// <summary>
        /// The moniker of the versioned state.
        /// It is used to identify the versioned state.
        /// </summary>
        Text Moniker => string.Empty;

        /// <summary>
        /// The version of the versioned state.
        /// It is used to identify the versioned state.
        /// </summary>
        Integer Version => -1;

        /// <summary>
        /// The data of the versioned state.
        /// It stores the actual data of the versioned state.
        /// </summary>
        IValue Data { get; }

        /// <summary>
        /// The <see cref="IVersionedState"/> implement this method directly.
        /// It is used to serialize the versioned state to <see cref="IValue"/>.
        /// It returns <see cref="List"/> of <see cref="Moniker"/>, <see cref="Version"/>,
        /// <see cref="Data"/>.
        /// </summary>
        IValue Serialize() => Serialize(Moniker, Version, Data);

        /// <summary>
        ///
        /// </summary>
        /// <param name="moniker"></param>
        /// <param name="version"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        static IValue Serialize(Text moniker, Integer version, IValue data)
        {
            if (string.IsNullOrEmpty(moniker))
            {
                throw new ArgumentException(
                    $"Serialized value's moniker must not be" +
                    $" null or empty, but {moniker}.");
            }

            if (version < 0)
            {
                throw new ArgumentException(
                    $"Serialized value's version must be" +
                    $" greater than or equal to 0, but {version}.");
            }

            return new List(
                moniker,
                version,
                data);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="serialized"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        static (Text moniker, Integer version, IValue data) Deconstruct(IValue serialized)
        {
            if (serialized is null)
            {
                throw new ArgumentNullException(nameof(serialized));
            }

            if (!(serialized is List list))
            {
                throw new ArgumentException(
                    "Serialized value must be a 'BenCodex.Types.List'.");
            }

            if (list.Count != 3)
            {
                throw new ArgumentException(
                    $"Serialized value must have 3 elements, but has {list.Count}.");
            }

            try
            {
                var moniker = (Text)list[0];
                if (string.IsNullOrEmpty(moniker))
                {
                    throw new ArgumentException(
                        $"Serialized value's moniker must not be" +
                        $" null or empty, but {moniker}.");
                }

                var version = (Integer)list[1];
                if (version < 0)
                {
                    throw new ArgumentException(
                        $"Serialized value's version must be" +
                        $" greater than or equal to 0, but {version}.");
                }

                var data = list[2];
                return (moniker, version, data);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Serialized value is invalid.", e);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="serialized"></param>
        /// <param name="moniker"></param>
        /// <param name="version"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        static bool TryDeconstruct(
            IValue serialized,
            out Text moniker,
            out Integer version,
            out IValue data)
        {
            try
            {
                (moniker, version, data) = Deconstruct(serialized);
                return true;
            }
            catch
            {
                (moniker, version, data) = (string.Empty, -1, null);
                return false;
            }
        }
    }

    public static class VersionedStateExtensions
    {
        public static IValue Serialize<T>(this T versionedState)
            where T : IVersionedState =>
            versionedState.Serialize();
    }
}
