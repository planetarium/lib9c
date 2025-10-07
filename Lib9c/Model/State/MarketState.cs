using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Crypto;

namespace Lib9c.Model.State
{
    public class MarketState
    {
        public List<Address> AvatarAddresses = new List<Address>();

        public MarketState(IValue rawList)
        {
            AvatarAddresses = rawList.ToList(StateExtensions.ToAddress);
        }

        public MarketState()
        {
        }

        public IValue Serialize()
        {
            return AvatarAddresses
                .Aggregate(
                    List.Empty,
                    (current, avatarAddress) => current.Add(avatarAddress.Serialize())
                );
        }
    }
}
