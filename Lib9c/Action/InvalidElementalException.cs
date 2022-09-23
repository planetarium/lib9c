using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
{
    [Serializable]
    public class InvalidElementalException : Exception
    {
        public InvalidElementalException()
        {
        }

        public InvalidElementalException(string message) : base(message)
        {
        }

        protected InvalidElementalException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}