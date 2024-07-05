using System;
using Bencodex.Types;
using Libplanet.Types.Assets;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Mail
{
    public class AdventureBossRaffleWinnerMail : Mail
    {
        public readonly long Season;
        public FungibleAssetValue Reward;

        public AdventureBossRaffleWinnerMail(long blockIndex, Guid id, long requiredBlockIndex, long season, FungibleAssetValue reward) : base(blockIndex, id, requiredBlockIndex)
        {
            Season = season;
            Reward = reward;
        }

        public AdventureBossRaffleWinnerMail(Dictionary serialized) : base(serialized)
        {
            Season = (Integer)serialized["s"];
            Reward = serialized["r"].ToFungibleAssetValue();
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        protected override string TypeId => nameof(AdventureBossRaffleWinnerMail);

        public override IValue Serialize() => ((Dictionary)base.Serialize())
            .Add("s", Season)
            .Add("r", Reward.Serialize());
    }
}
