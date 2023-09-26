﻿using System;
using System.Runtime.Serialization;

namespace Nekoyume.Model.Exceptions
{
    [Serializable]
    public class InvalidAddressException : InvalidOperationException
    {
        public InvalidAddressException()
        {
        }

        public InvalidAddressException(string msg) : base(msg)
        {
        }

        protected InvalidAddressException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
