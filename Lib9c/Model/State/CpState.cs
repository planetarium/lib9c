using System;
using Bencodex;
using Bencodex.Types;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// CpState is a state that stores the cp of the player.
    /// </summary>
    public class CpState : IBencodable, IState
    {
        public int Cp;

        public IValue Bencoded => List.Empty.Add(Cp.Serialize());

        public CpState(int cp)
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

            Cp = l[0].ToInteger();
        }

        public IValue Serialize() => Bencoded;
    }
}
