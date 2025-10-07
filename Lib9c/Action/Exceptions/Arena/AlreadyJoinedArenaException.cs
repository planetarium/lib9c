using System;
using System.Runtime.Serialization;
using Libplanet.Crypto;

namespace Lib9c.Action.Exceptions.Arena
{
    [Serializable]
    public class AlreadyJoinedArenaException : Exception
    {
        public AlreadyJoinedArenaException(
            int championshipId,
            int round,
            Address avatarAddress) :
            base(
                $"Avatar {avatarAddress} has already joined the arena for championship {championshipId}, round {round}.")
        {
        }

        public AlreadyJoinedArenaException(string msg) : base(msg)
        {
        }

        protected AlreadyJoinedArenaException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
