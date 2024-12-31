using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    /// <summary>
    /// Represents an exception that is thrown when an invalid item ID is used.
    /// </summary>
    [Serializable]
    public class InvalidItemIdException : ArgumentOutOfRangeException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidItemIdException"/> class.
        /// </summary>
        public InvalidItemIdException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidItemIdException"/> class with a specified error message.
        /// </summary>
        /// <param name="msg">The message that describes the error.</param>
        public InvalidItemIdException(string msg) : base(msg)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidItemIdException"/> class with a specified error message
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected InvalidItemIdException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
