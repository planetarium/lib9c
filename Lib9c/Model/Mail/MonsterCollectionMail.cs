using System;
using Bencodex.Types;
using Lib9c.Action;

namespace Lib9c.Model.Mail
{
    [Serializable]
    public class MonsterCollectionMail : AttachmentMail
    {
        public MonsterCollectionMail(AttachmentActionResult attachmentActionResult, long blockIndex, Guid id, long requiredBlockIndex)
            : base(attachmentActionResult, blockIndex, id, requiredBlockIndex)
        {
        }

        public MonsterCollectionMail(Dictionary serialized) : base(serialized)
        {
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        protected override string TypeId => "monsterCollectionMail";
        public override MailType MailType => MailType.System;
    }
}
