using System;
using System.Runtime.Serialization;

namespace Lib9c.Action
{
    [Serializable]
    public class FailedLoadSheetException : Exception
    {
        public FailedLoadSheetException(string message) : base(message)
        {
        }

        public FailedLoadSheetException(Type sheetType) : base($"{sheetType.FullName}")
        {
        }

        protected FailedLoadSheetException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
