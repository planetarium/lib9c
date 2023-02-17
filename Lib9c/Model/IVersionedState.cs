using Bencodex.Types;

namespace Nekoyume.Model
{
    /// <summary>
    /// Interface for versioned state.
    /// </summary>
    public interface IVersionedState
    {
        Text Moniker { get; }
        Integer Version { get; }
        IValue Data { get; }
    }
}
