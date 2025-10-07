using System;
using Bencodex;
using Bencodex.Types;

namespace Lib9c.Model.State
{
    /// <summary>
    /// CpState is a state that stores the cp of the player.
    /// </summary>
    public class CpState : IBencodable, IState
    {
        public long Cp;

        public IValue Bencoded => List.Empty.Add(Cp.Serialize());

        public CpState(long cp)
        {
            Cp = cp;
        }

        public CpState(IValue bencoded)
        {
            if (bencoded is not List l)
            {
                throw new ArgumentException(
                    $"Invalid bencoded value: {bencoded.Inspect()}",
                    nameof(bencoded)
                );
            }

            Cp = l[0].ToLong();
        }

        public IValue Serialize() => Bencoded;
    }
}
