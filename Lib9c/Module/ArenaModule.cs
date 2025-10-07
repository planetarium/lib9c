using Bencodex.Types;
using Lib9c.Model.Arena;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.Module
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
            int championshipId,
            int round,
            Address avatarAddress,
            IValue arenaParticipantState)
        {
            var accountAddress = Addresses.GetArenaParticipantAccountAddress(championshipId, round);
            return world.SetArenaParticipant(accountAddress, avatarAddress, arenaParticipantState);
        }

        public static IWorld SetArenaParticipant(
            this IWorld world,
            Address accountAddress,
            Address stateAddress,
            IValue arenaParticipantState)
        {
            var account = world
                .GetAccount(accountAddress)
                .SetState(stateAddress, arenaParticipantState);
            return world.SetAccount(accountAddress, account);
        }

        public static ArenaParticipant GetArenaParticipant(
            this IWorldState worldState,
            int championshipId,
            int round,
            Address avatarAddress)
        {
            var accountAddress = Addresses.GetArenaParticipantAccountAddress(championshipId, round);
            var state = worldState.GetArenaParticipantState(accountAddress, avatarAddress);
            return state is null
                ? null
                : new ArenaParticipant(state);
        }

        public static IValue GetArenaParticipantState(
            this IWorldState worldState,
            int championshipId,
            int round,
            Address avatarAddress)
        {
            var accountAddress = Addresses.GetArenaParticipantAccountAddress(championshipId, round);
            return worldState.GetArenaParticipantState(accountAddress, avatarAddress);
        }

        public static IValue GetArenaParticipantState(
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
