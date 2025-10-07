using System;
using Bencodex.Types;
using Lib9c.Model.State;
using static Lib9c.SerializeKeys;

namespace Lib9c.Model.Mail
{
    [Serializable]
    public class OrderBuyerMail : Mail
    {
        public readonly Guid OrderId;
        public OrderBuyerMail(long blockIndex, Guid id, long requiredBlockIndex, Guid orderId) : base(blockIndex, id, requiredBlockIndex)
        {
            OrderId = orderId;
        }

        public OrderBuyerMail(Dictionary serialized) : base(serialized)
        {
            OrderId = serialized[OrderIdKey].ToGuid();
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        public override MailType MailType => MailType.Auction;

        protected override string TypeId => nameof(OrderBuyerMail);

        public override IValue Serialize() => ((Dictionary)base.Serialize())
            .Add(OrderIdKey, OrderId.Serialize());
    }
}
