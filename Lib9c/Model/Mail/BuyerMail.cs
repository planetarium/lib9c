using System;
using Lib9c.Action;

namespace Lib9c.Model.Mail
{
    [Serializable]
    public class BuyerMail : AttachmentMail
    {
        protected override string TypeId => "buyerMail";
        public override MailType MailType => MailType.Auction;

        public BuyerMail(AttachmentActionResult attachmentActionResult, long blockIndex, Guid id, long requiredBlockIndex)
            : base(attachmentActionResult, blockIndex, id, requiredBlockIndex)
        {

        }

        public BuyerMail(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
