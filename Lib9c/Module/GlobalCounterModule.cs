using System.Globalization;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;

namespace Nekoyume.Module
{
    /// <summary>
    /// Reads and writes the global counters that drive hard world openings.
    /// </summary>
    public static class GlobalCounterModule
    {
        /// <summary>
        /// The state key of counter <paramref name="id"/>.
        /// </summary>
        public static Address CounterKey(int id) =>
            Addresses.GlobalCounter.Derive($"counter_{id.ToString(CultureInfo.InvariantCulture)}");

        /// <summary>
        /// The value of counter <paramref name="id"/>, or 0 if it was never written.
        /// </summary>
        public static long GetCounter(this IWorldState worldState, int id) =>
            worldState.GetAccountState(Addresses.GlobalCounter).GetState(CounterKey(id)) is Integer value
                ? value
                : 0L;

        /// <summary>
        /// Adds <paramref name="delta"/> to counter <paramref name="id"/>.
        /// </summary>
        public static IWorld AddCounter(this IWorld world, int id, long delta)
        {
            var account = world.GetAccount(Addresses.GlobalCounter);
            account = account.SetState(CounterKey(id), (Integer)(world.GetCounter(id) + delta));
            return world.SetAccount(Addresses.GlobalCounter, account);
        }
    }
}
