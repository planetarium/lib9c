using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;

namespace Nekoyume.Model.State
{
    public class MarketState
    {
        public List<Address> AvatarAddressList = new List<Address>();

        public MarketState(IValue rawList)
        {
            AvatarAddressList = rawList.ToList(StateExtensions.ToAddress);
        }

        public MarketState()
        {
        }

        public IValue Serialize()
        {
            return AvatarAddressList
                .Aggregate(
                    List.Empty,
                    (current, avatarAddress) => current.Add(avatarAddress.Serialize())
                );
        }
    }
}
