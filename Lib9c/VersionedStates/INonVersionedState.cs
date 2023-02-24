using Bencodex.Types;

namespace Nekoyume.VersionedStates
{
    /// <summary>
    /// Interface for non-versioned state.
    /// </summary>
    public interface INonVersionedState
    {
        /// <summary>
        /// The state data.
        /// </summary>
        IValue Data { get; }

        IValue Serialize() => Serialize(this);

        static IValue Serialize(INonVersionedState nonVersionedState) =>
            nonVersionedState.Data;
    }

    public static class NonVersionedStateExtensions
    {
        public static IValue Serialize<T>(this T nonVersionedState)
            where T : INonVersionedState =>
            nonVersionedState.Serialize();
    }
}
