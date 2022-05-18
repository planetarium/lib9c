using System;
using System.Runtime.Serialization;

namespace Nekoyume.Model.Arena
{
    [Serializable]
    public class RoundNotFoundByBlockIndexException : Exception
    {
        public RoundNotFoundByBlockIndexException(string message) : base(message)
        {

        }

        protected RoundNotFoundByBlockIndexException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class RoundNotFoundByIdsException : Exception
    {
        public RoundNotFoundByIdsException(string message) : base(message)
        {

        }

        protected RoundNotFoundByIdsException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class ArenaScoreAlreadyContainsException : Exception
    {
        public ArenaScoreAlreadyContainsException(string message) : base(message)
        {
        }

        protected ArenaScoreAlreadyContainsException(SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class ArenaInformationAlreadyContainsException : Exception
    {
        public ArenaInformationAlreadyContainsException(string message) : base(message)
        {
        }

        protected ArenaInformationAlreadyContainsException(SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
