﻿using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
{
    public class InvalidTradableItemException : InvalidOperationException
    {
        public InvalidTradableItemException(string message) : base(message)
        {
        }

        protected InvalidTradableItemException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
