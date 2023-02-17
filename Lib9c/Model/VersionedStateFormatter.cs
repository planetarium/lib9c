#nullable enable
using System;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model
{
    public static class VersionedStateFormatter
    {
        public static IValue Serialize(Text moniker, Integer version, IValue data)
        {
            if (string.IsNullOrEmpty(moniker))
            {
                throw new ArgumentException(
                    $"{nameof(moniker)} must not be null or empty.");
            }

            return new List(
                moniker,
                version,
                data ?? Null.Value);
        }

        public static IValue Serialize(string moniker, uint version, IValue data) =>
            Serialize((Text)moniker, (Integer)version, data ?? Null.Value);

        public static IValue Serialize(string moniker, uint version, IState state) =>
            Serialize((Text)moniker, (Integer)version, state?.Serialize() ?? Null.Value);

        public static (Text moniker, Integer version, IValue data) Deconstruct(IValue serialized)
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

                return (moniker, version, list[2]);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Serialized value is invalid.", e);
            }
        }

        public static bool TryDeconstruct(
            IValue serialized,
            out (Text moniker, Integer version, IValue data)? result)
        {
            try
            {
                result = Deconstruct(serialized);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        public static (Text moniker, Integer version, IValue data) Deconstruct<T>(
            T state,
            out IValue? serialized)
            where T : IState
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            serialized = state.Serialize();
            if (TryDeconstruct(serialized, out var result) &&
                result.HasValue)
            {
                return result.Value;
            }

            if (!VersionedStateImplAttribute.TryGetFrom(typeof(T), out var attr))
            {
                throw new ArgumentException(
                    $"Type {typeof(T).FullName} must have" +
                    $" {nameof(VersionedStateImplAttribute)}.",
                    new DeconstructVersionedStateException(serialized: serialized));
            }

            if (!VersionedStateAttribute.TryGetFrom(attr!.Type, out var attr2))
            {
                throw new ArgumentException(
                    $"Type {attr.Type.FullName} must have" +
                    $" {nameof(VersionedStateAttribute)}.",
                    new DeconstructVersionedStateException(serialized: serialized));
            }

            return (attr2!.Moniker, attr2.Version, serialized);
        }

        public static (Text moniker, Integer version, IValue data) Deconstruct<T>(T state)
            where T : IState
        {
            return Deconstruct(state, out _);
        }

        public static bool TryDeconstruct<T>(
            T state,
            out (Text moniker, Integer version, IValue data)? result,
            out IValue? serialized)
            where T : IState
        {
            try
            {
                result = Deconstruct(state, out serialized);
                return true;
            }
            catch (ArgumentException e)
            {
                result = null;
                serialized = e.InnerException is DeconstructVersionedStateException e2
                    ? e2.Serialized
                    : null;
                return false;
            }
        }

        public static bool TryDeconstruct<T>(
            T state,
            out (Text moniker, Integer version, IValue data)? result)
            where T : IState
        {
            return TryDeconstruct(state, out result, out _);
        }

        public static bool ValidateFormat(IValue serialized)
        {
            if (!(serialized is List list))
            {
                return false;
            }

            if (list.Count != 3)
            {
                return false;
            }

            if (!(list[0] is Text moniker) ||
                string.IsNullOrEmpty(moniker))
            {
                return false;
            }

            if (!(list[1] is Integer version) ||
                version < 0)
            {
                return false;
            }

            return true;
        }
    }

    public class DeconstructVersionedStateException : Exception
    {
        public IValue? Serialized { get; }

        public DeconstructVersionedStateException(
            string? message = null,
            Exception? innerException = null,
            IValue? serialized = null)
            : base(message, innerException)
        {
            Serialized = serialized;
        }
    }
}
