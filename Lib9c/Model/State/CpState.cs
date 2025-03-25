using System;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.EnumType;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// CpState is a state that stores the cp of the player.
    /// </summary>
    public class CpState : IBencodable, IState
    {
        public readonly static Address AccountAddress = Addresses.CpState;

        public static Address DeriveAddress(Address avatarAddress, BattleType battleType) =>
            avatarAddress.Derive($"cp_state_{battleType}");

        public int Cp;
        public Address AvatarAddress;

        public IValue Bencoded => List.Empty.Add(AvatarAddress.Serialize()).Add(Cp.Serialize());

        public CpState(Address avatarAddress, int cp)
        {
            AvatarAddress = avatarAddress;
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

            AvatarAddress = l[0].ToAddress();
            Cp = l[1].ToInteger();
        }

        public IValue Serialize() => Bencoded;
    }
}
