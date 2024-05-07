using System.Collections.Generic;
using System.Linq;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Exceptions.AdventureBoss;
using Nekoyume.Model.State;

namespace Nekoyume.Model.AdventureBoss
{
    public class BountyBoard : IBencodable
    {
        public List<Investor> Investors = new();

        public BountyBoard()
        {
        }

        public BountyBoard(IValue bencoded)
        {
            Investors = ((List)bencoded).ToList(i => new Investor(i));
        }

        public void AddOrUpdate(Address avatarAddress, FungibleAssetValue price)
        {
            var investor = Investors.FirstOrDefault(i => i.AvatarAddress.Equals(avatarAddress));
            if (investor is null)
            {
                Investors.Add(new Investor(avatarAddress, price));
            }
            else
            {
                if (investor.Count == Investor.MaxInvestmentCount)
                {
                    throw new MaxInvestmentCountExceededException(
                        $"Avatar {avatarAddress} already invested {investor.Count} times.");
                }
                investor.Price += price;
                investor.Count++;
            }
        }

        public IValue Bencoded => new List(Investors.Select(i => i.Bencoded));
    }
}
