using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Types.Assets;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Mail
{
    public class PatrolRewardMail : Mail
    {
        public override MailType MailType => MailType.Auction;

        public List<FungibleAssetValue> FungibleAssetValues = new ();
        public List<(int id, int count)> Items = new ();

        public PatrolRewardMail(long blockIndex, Guid id, long requiredBlockIndex,
            List<FungibleAssetValue> fungibleAssetValues, List<(int id, int count)> items)
            : base(blockIndex, id, requiredBlockIndex)
        {
            FungibleAssetValues = fungibleAssetValues;
            Items = items;
        }

        public PatrolRewardMail(Dictionary serialized) : base(serialized)
        {
            if (serialized.ContainsKey("f"))
            {
                FungibleAssetValues = serialized["f"].ToList(StateExtensions.ToFungibleAssetValue);
            }

            if (serialized.ContainsKey("i"))
            {
                Items = serialized["i"].ToList<(int, int)>(v =>
                {
                    var list = (List) v;
                    return ((Integer)list[0], (Integer)list[1]);
                });
            }
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        protected override string TypeId => nameof(PatrolRewardMail);

        public override IValue Serialize()
        {
            var dict = (Dictionary)base.Serialize();
            if (FungibleAssetValues.Any())
            {
                dict = dict.SetItem("f", new List(FungibleAssetValues.Select(f => f.Serialize())));
            }

            if (Items.Any())
            {
                dict = dict.SetItem("i",
                    new List(Items.Select(tuple => List.Empty.Add(tuple.id).Add(tuple.count))));
            }

            return dict;
        }

    }
}
