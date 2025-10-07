using System;
using Bencodex.Types;
using Lib9c.Action;

namespace Lib9c.Model.Mail
{
    [Serializable]
    public class SellerMail : AttachmentMail
    {
        protected override string TypeId => "seller";
        public override MailType MailType => MailType.Auction;

        public SellerMail(AttachmentActionResult attachmentActionResult, long blockIndex, Guid id, long requiredBlockIndex) : base(attachmentActionResult,
            blockIndex, id, requiredBlockIndex)
        {
        }

        public SellerMail(Dictionary serialized) : base(serialized)
        {
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
