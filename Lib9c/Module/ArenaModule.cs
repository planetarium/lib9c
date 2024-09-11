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
            var accountAddress = Addresses.GetArenaParticipantAccountAddress(championshipId, round);
            return world.SetArenaParticipant(accountAddress, avatarAddress, arenaParticipant.Bencoded);
        }

        public static IWorld SetArenaParticipant(
            this IWorld world,
            Address accountAddress,
            Address stateAddress,
            IValue arenaParticipant)
        {
            var account = world
                .GetAccount(accountAddress)
                .SetState(stateAddress, arenaParticipant);
            return world.SetAccount(accountAddress, account);
        }

        public static ArenaParticipant GetArenaParticipant(
            this IWorldState worldState,
            int championshipId,
            int round,
            Address avatarAddress)
        {
            var accountAddress = Addresses.GetArenaParticipantAccountAddress(championshipId, round);
            var state = worldState.GetArenaParticipant(accountAddress, avatarAddress);
            return state is null
                ? null
                : new ArenaParticipant(state);
        }

        public static IValue GetArenaParticipant(
            this IWorldState worldState,
            Address accountAddress,
            Address stateAddress)
        {
            return worldState
                .GetAccountState(accountAddress)
                .GetState(stateAddress);
        }
    }
}
