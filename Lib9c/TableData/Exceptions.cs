using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Lib9c.TableData
{
    [Serializable]
    public class SheetRowColumnException : Exception
    {
        public SheetRowColumnException(string message) : base(message)
        {
        }

        protected SheetRowColumnException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class SheetRowValidateException : Exception
    {
        public SheetRowValidateException(string message) : base(message)
        {
        }

        protected SheetRowValidateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class SheetRowNotFoundException : Exception
    {
        public SheetRowNotFoundException(string sheetName, int intKey)
            : this(sheetName, intKey.ToString(CultureInfo.InvariantCulture))
        {
        }

        public SheetRowNotFoundException(string sheetName, long longKey)
            : this(sheetName, longKey.ToString(CultureInfo.InvariantCulture))
        {
        }

        public SheetRowNotFoundException(string sheetName, string key)
            : this(sheetName, "Key", key)
        {
        }

        public SheetRowNotFoundException(string sheetName, string condition, string value) :
            this($"{sheetName}: {condition} - {value}")
        {
        }

        public SheetRowNotFoundException(string addressesHex, string sheetName, int intKey) :
            this($"{addressesHex}{sheetName}:" +
                $" Key - {intKey.ToString(CultureInfo.InvariantCulture)}")
        {
        }

        public SheetRowNotFoundException(
            string actionType,
            string addressesHex,
            string sheetName,
            int intKey) :
            this($"[{actionType}][{addressesHex}]{sheetName}:" +
                $" Key - {intKey.ToString(CultureInfo.InvariantCulture)}")
        {
        }

        public SheetRowNotFoundException(string message) : base(message)
        {
        }

        protected SheetRowNotFoundException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }
}
