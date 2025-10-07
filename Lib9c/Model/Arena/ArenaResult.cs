using System;
using Bencodex;
using Bencodex.Types;
using Lib9c.Action;
using Lib9c.Model.State;
using Libplanet.Crypto;
using Libplanet.Types.Tx;

namespace Lib9c.Model.Arena
{
    /// <summary>
    /// temp
    /// </summary>
    public class ArenaResult : IBencodable, IState
    {
        public static Address DeriveAddress(Address avatarAddress, TxId txId) =>
            avatarAddress.Derive(txId.ToString());

        public bool IsVictory;
        public int PortraitId;
        public int Level;
        public long Cp;

        public IValue Bencoded => List.Empty.Add(IsVictory).Add(PortraitId).Add(Level).Add(Cp);

        public ArenaResult(bool isVictory, int portraitId, int level, long cp)
        {
            IsVictory = isVictory;
            PortraitId = portraitId;
            Level = level;
            Cp = cp;
        }

        public ArenaResult(IValue bencoded)
        {
            if (bencoded is not List l)
            {
                throw new ArgumentException(
                    $"Invalid bencoded value: {bencoded.Inspect()}",
                    nameof(bencoded)
                );
            }

            IsVictory = l[0].ToBoolean();
            PortraitId = (Integer)l[1];
            Level = (Integer)l[2];
            Cp = (Integer)l[3];
        }

        public IValue Serialize() => Bencoded;
    }
}
