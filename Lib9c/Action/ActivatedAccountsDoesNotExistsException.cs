using System;
using System.Runtime.Serialization;

#nullable disable
namespace Nekoyume.Action
{
    [Serializable]
    public class ActivatedAccountsDoesNotExistsException : ActivationException
    {
        public ActivatedAccountsDoesNotExistsException()
        {
        }

        public ActivatedAccountsDoesNotExistsException(
            SerializationInfo info, StreamingContext context
        ) : base(info, context)
        {
        }
    }
}
