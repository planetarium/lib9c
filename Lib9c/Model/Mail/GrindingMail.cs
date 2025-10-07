using System;
using Bencodex.Types;
using Lib9c.Model.State;
using Libplanet.Types.Assets;

namespace Lib9c.Model.Mail
{
    public class GrindingMail : Mail
    {
        public readonly int ItemCount;
        public FungibleAssetValue Asset;
        public readonly int RewardMaterialCount;
        public GrindingMail(long blockIndex, Guid id, long requiredBlockIndex, int itemCount, FungibleAssetValue asset, int rewardMaterialCount) : base(blockIndex, id, requiredBlockIndex)
        {
            ItemCount = itemCount;
            Asset = asset;
            RewardMaterialCount = rewardMaterialCount;
        }

        public GrindingMail(Dictionary serialized) : base(serialized)
        {
            ItemCount = serialized["ic"].ToInteger();
            Asset = serialized["a"].ToFungibleAssetValue();
            RewardMaterialCount = serialized.TryGetValue((Text)"rmc", out var value)
                ? (Integer)value
                : 0;
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        public override MailType MailType => MailType.Grinding;

        protected override string TypeId => nameof(GrindingMail);

        public override IValue Serialize() => ((Dictionary)base.Serialize())
            .Add("ic", ItemCount.Serialize())
            .Add("a", Asset.Serialize())
            .Add("rmc", RewardMaterialCount);
    }
}
