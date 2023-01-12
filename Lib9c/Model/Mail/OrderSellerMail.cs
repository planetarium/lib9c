using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.State;
using static Lib9c.SerializeKeys;

namespace Lib9c.Model.Mail
{
    [Serializable]
    public class OrderSellerMail : Mail
    {
        public readonly Guid OrderId;
        public OrderSellerMail(long blockIndex, Guid id, long requiredBlockIndex, Guid orderId) : base(blockIndex, id, requiredBlockIndex)
        {
            OrderId = orderId;
        }

        public OrderSellerMail(Dictionary serialized) : base(serialized)
        {
            OrderId = serialized[OrderIdKey].ToGuid();
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        public override MailType MailType => MailType.Auction;

        protected override string TypeId => nameof(OrderSellerMail);

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)OrderIdKey] = OrderId.Serialize(),
            }.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002
    }
}
