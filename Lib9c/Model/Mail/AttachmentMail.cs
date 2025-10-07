using System;
using Bencodex.Types;
using Lib9c.Action;

namespace Lib9c.Model.Mail
{
    [Serializable]
    public abstract class AttachmentMail : Mail
    {
        public AttachmentActionResult attachment;

        protected AttachmentMail(AttachmentActionResult attachmentActionResult, long blockIndex, Guid id, long requiredBlockIndex)
            : base(blockIndex, id, requiredBlockIndex)
        {
            attachment = attachmentActionResult;
        }

        public AttachmentMail(Dictionary serialized)
            : base(serialized)
        {
            attachment = AttachmentActionResult.Deserialize(
                (Dictionary)serialized["attachment"]
            );
        }

        public override IValue Serialize() => ((Dictionary)base.Serialize())
            .Add("attachment", attachment.Serialize());
    }
}
