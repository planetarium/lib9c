using System;
using Bencodex.Types;
using Nekoyume.Model.Item;

namespace Nekoyume.Model.Mail
{
    public class CustomCraftMail: Mail
    {
        public Equipment Equipment;

        public override MailType MailType => MailType.CustomCraft;

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        protected override string TypeId { get; }

        public CustomCraftMail(long blockIndex, Guid id, long requiredBlockIndex, Equipment equipment) : base(blockIndex, id, requiredBlockIndex)
        {
            Equipment = equipment;
        }

        public CustomCraftMail(Dictionary serialized) : base(serialized)
        {
            Equipment = new Equipment((Dictionary)serialized["equipment"]);
        }

        public override IValue Serialize() =>
            ((Dictionary)base.Serialize()).Add("equipment", Equipment.Serialize());
    }
}
