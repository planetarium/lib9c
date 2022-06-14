using System;
using System.Runtime.Serialization;

namespace Nekoyume.Model.Arena
{
    [Serializable]
    public class RoundNotFoundException : Exception
    {
        public RoundNotFoundException(string message) : base(message)
        {
        }

        protected RoundNotFoundException(SerializationInfo info, StreamingContext context) :
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

    [Serializable]
    public class ArenaBoardInformationAlreadyContainsException : Exception
    {
        public ArenaBoardInformationAlreadyContainsException(string message) : base(message)
        {
        }

        protected ArenaBoardInformationAlreadyContainsException(SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class ArenaParticipantsNotFoundException : Exception
    {
        public ArenaParticipantsNotFoundException(string message) : base(message)
        {
        }

        protected ArenaParticipantsNotFoundException(SerializationInfo info,
            StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class ArenaAvatarStateNotFoundException : Exception
    {
        public ArenaAvatarStateNotFoundException(string message) : base(message)
        {
        }

        protected ArenaAvatarStateNotFoundException(SerializationInfo info,
            StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class ArenaScoreNotFoundException : Exception
    {
        public ArenaScoreNotFoundException(string message) : base(message)
        {
        }

        protected ArenaScoreNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class ArenaInformationNotFoundException : Exception
    {
        public ArenaInformationNotFoundException(string message) : base(message)
        {
        }

        protected ArenaInformationNotFoundException(SerializationInfo info,
            StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class ArenaBoardInformationNotFoundException : Exception
    {
        public ArenaBoardInformationNotFoundException(string message) : base(message)
        {
        }

        protected ArenaBoardInformationNotFoundException(SerializationInfo info,
            StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class AddressNotFoundInArenaParticipantsException : Exception
    {
        public AddressNotFoundInArenaParticipantsException(string message) : base(message)
        {
        }

        protected AddressNotFoundInArenaParticipantsException(SerializationInfo info,
            StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class NotEnoughTicketException : Exception
    {
        public NotEnoughTicketException(string message) : base(message)
        {
        }

        protected NotEnoughTicketException(SerializationInfo info,
            StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class ValidateScoreDifferenceException : Exception
    {
        public ValidateScoreDifferenceException(string message) : base(message)
        {
        }

        protected ValidateScoreDifferenceException(SerializationInfo info,
            StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class ThisArenaIsClosedException : Exception
    {
        public ThisArenaIsClosedException(string message) : base(message)
        {
        }

        protected ThisArenaIsClosedException(SerializationInfo info,
            StreamingContext context) :
            base(info, context)
        {
        }
    }

}
