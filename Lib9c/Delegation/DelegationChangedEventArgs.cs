#nullable enable
using System;

namespace Nekoyume.Delegation
{
    public sealed class DelegationChangedEventArgs : EventArgs
    {
        public DelegationChangedEventArgs(long height)
        {
            Height = height;
        }

        public long Height { get; }
    }
}
