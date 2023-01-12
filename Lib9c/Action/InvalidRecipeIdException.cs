using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class InvalidRecipeIdException: Exception
    {
        public InvalidRecipeIdException()
        {
        }

        public InvalidRecipeIdException(string msg) : base(msg)
        {
        }

        protected InvalidRecipeIdException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

    }
}
