using System;
using Lib9c.Action;

namespace Lib9c.Model.Mail
{
    [Serializable]
    public class DailyRewardMail : AttachmentMail
    {
        protected override string TypeId => "dailyRewardMail";
        public override MailType MailType => MailType.System;

        public DailyRewardMail(AttachmentActionResult attachmentActionResult, long blockIndex, Guid id, long requiredBlockIndex)
            : base(attachmentActionResult, blockIndex, id, requiredBlockIndex)
        {

        }

        public DailyRewardMail(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
