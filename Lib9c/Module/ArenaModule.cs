using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.Arena;

namespace Nekoyume.Module
{
    public static class ArenaModule
    {
        public static IWorld SetArenaParticipant(
            this IWorld world,
            int championshipId,
            int round,
            Address avatarAddress,
            ArenaParticipant arenaParticipant)
        {
            var stateAddress = ArenaParticipant.DeriveAddress(championshipId, round, avatarAddress);
            return world.SetArenaParticipant(stateAddress, arenaParticipant.Bencoded);
        }

        public static IWorld SetArenaParticipant(
            this IWorld world,
            Address stateAddress,
            IValue arenaParticipant)
        {
            var account = world
                .GetAccount(Addresses.ArenaParticipant)
                .SetState(stateAddress, arenaParticipant);
            return world.SetAccount(Addresses.ArenaParticipant, account);
        }

        public static ArenaParticipant GetArenaParticipant(
            this IWorldState worldState,
            int championshipId,
            int round,
            Address avatarAddress)
        {
            var stateAddress = ArenaParticipant.DeriveAddress(championshipId, round, avatarAddress);
            var state = worldState.GetArenaParticipant(stateAddress);
            return state is null
                ? null
                : new ArenaParticipant(state);
        }

        public static IValue GetArenaParticipant(
            this IWorldState worldState,
            Address stateAddress)
        {
            return worldState
                .GetAccountState(Addresses.ArenaParticipant)
                .GetState(stateAddress);
        }
    }
}
