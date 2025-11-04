using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Nekoyume.Model.EnumType;

namespace Nekoyume.Model.Rune
{
    [Serializable]
    public class RuneNotFoundException : Exception
    {
        public RuneNotFoundException(string message) : base(message)
        {
        }

        protected RuneNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class RuneCostNotFoundException : Exception
    {
        public RuneCostNotFoundException(string message) : base(message)
        {
        }

        protected RuneCostNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class RuneCostDataNotFoundException : Exception
    {
        public RuneCostDataNotFoundException(string message) : base(message)
        {
        }

        protected RuneCostDataNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class TryCountIsZeroException : Exception
    {
        public TryCountIsZeroException(string msg) : base(msg)
        {
        }

        public TryCountIsZeroException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class RuneListNotFoundException : Exception
    {
        public RuneListNotFoundException(string message) : base(message)
        {
        }

        protected RuneListNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class RuneStateNotFoundException : Exception
    {
        public RuneStateNotFoundException(string message) : base(message)
        {
        }

        protected RuneStateNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class SlotNotFoundException : Exception
    {
        public SlotNotFoundException(string message) : base(message)
        {
        }

        protected SlotNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class SlotIsLockedException : Exception
    {
        public SlotIsLockedException(string message) : base(message)
        {
        }

        protected SlotIsLockedException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class SlotIsAlreadyUnlockedException : Exception
    {
        public SlotIsAlreadyUnlockedException(string message) : base(message)
        {
        }

        protected SlotIsAlreadyUnlockedException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class SlotRuneTypeException : Exception
    {
        public SlotRuneTypeException(string message) : base(message)
        {
        }

        protected SlotRuneTypeException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class IsEquippableRuneException : Exception
    {
        public IsEquippableRuneException(string message) : base(message)
        {
        }

        protected IsEquippableRuneException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class RuneInfosIsEmptyException : Exception
    {
        public RuneInfosIsEmptyException(string message) : base(message)
        {
        }

        protected RuneInfosIsEmptyException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class DuplicatedRuneSlotIndexException : Exception
    {

        public DuplicatedRuneSlotIndexException(string message) : base(message)
        {
        }

        protected DuplicatedRuneSlotIndexException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class DuplicatedRuneIdException : Exception
    {

        public DuplicatedRuneIdException(string message) : base(message)
        {
        }

        protected DuplicatedRuneIdException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class IsUsableSlotException : Exception
    {

        public IsUsableSlotException(string message) : base(message)
        {
        }

        protected IsUsableSlotException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class MismatchRuneSlotTypeException : Exception
    {
        public MismatchRuneSlotTypeException(string message) : base(message)
        {
        }

        protected MismatchRuneSlotTypeException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class ForbiddenRuneEquippedException : Exception
    {
        public ForbiddenRuneEquippedException(string message) : base(message)
        {
        }

        public ForbiddenRuneEquippedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ForbiddenRuneEquippedException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }

    [Serializable]
    public class ForbiddenRuneTypeEquippedException : Exception
    {
        /// <summary>
        /// Gets the list of forbidden rune types that were equipped.
        /// </summary>
        public List<RuneType> ForbiddenRuneTypes { get; }

        /// <summary>
        /// Gets the list of equipped rune types that are forbidden.
        /// </summary>
        public List<RuneType> EquippedRuneTypes { get; }

        /// <summary>
        /// Initializes a new instance of the ForbiddenRuneTypeEquippedException class.
        /// </summary>
        public ForbiddenRuneTypeEquippedException()
        {
            ForbiddenRuneTypes = new List<RuneType>();
            EquippedRuneTypes = new List<RuneType>();
        }

        /// <summary>
        /// Initializes a new instance of the ForbiddenRuneTypeEquippedException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ForbiddenRuneTypeEquippedException(string message) : base(message)
        {
            ForbiddenRuneTypes = new List<RuneType>();
            EquippedRuneTypes = new List<RuneType>();
        }

        /// <summary>
        /// Initializes a new instance of the ForbiddenRuneTypeEquippedException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ForbiddenRuneTypeEquippedException(string message, Exception innerException) : base(message, innerException)
        {
            ForbiddenRuneTypes = new List<RuneType>();
            EquippedRuneTypes = new List<RuneType>();
        }

        /// <summary>
        /// Initializes a new instance of the ForbiddenRuneTypeEquippedException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="forbiddenRuneTypes">The list of forbidden rune types.</param>
        /// <param name="equippedRuneTypes">The list of equipped rune types that are forbidden.</param>
        public ForbiddenRuneTypeEquippedException(
            string message,
            List<RuneType> forbiddenRuneTypes,
            List<RuneType> equippedRuneTypes) : base(message)
        {
            ForbiddenRuneTypes = forbiddenRuneTypes ?? new List<RuneType>();
            EquippedRuneTypes = equippedRuneTypes ?? new List<RuneType>();
        }

        /// <summary>
        /// Initializes a new instance of the ForbiddenRuneTypeEquippedException class.
        /// </summary>
        /// <param name="forbiddenRuneTypes">The list of forbidden rune types.</param>
        /// <param name="equippedRuneTypes">The list of equipped rune types that are forbidden.</param>
        public ForbiddenRuneTypeEquippedException(
            List<RuneType> forbiddenRuneTypes,
            List<RuneType> equippedRuneTypes)
            : this(
                $"Forbidden rune type(s) equipped for this floor. Forbidden types: {string.Join(", ", forbiddenRuneTypes ?? new List<RuneType>())}, Equipped forbidden types: {string.Join(", ", equippedRuneTypes ?? new List<RuneType>())}",
                forbiddenRuneTypes,
                equippedRuneTypes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ForbiddenRuneTypeEquippedException class for serialization.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        protected ForbiddenRuneTypeEquippedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
            ForbiddenRuneTypes = (List<RuneType>)info.GetValue(nameof(ForbiddenRuneTypes), typeof(List<RuneType>)) ?? new List<RuneType>();
            EquippedRuneTypes = (List<RuneType>)info.GetValue(nameof(EquippedRuneTypes), typeof(List<RuneType>)) ?? new List<RuneType>();
        }

        /// <summary>
        /// Gets object data for serialization.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The streaming context.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ForbiddenRuneTypes), ForbiddenRuneTypes);
            info.AddValue(nameof(EquippedRuneTypes), EquippedRuneTypes);
        }
    }
}
