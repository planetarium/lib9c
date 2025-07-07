using System;
using Bencodex.Types;
using Nekoyume.Model.Item;

namespace Nekoyume.Model.Mail
{
    public class CustomCraftMail : Mail
    {
        public Equipment Equipment;

        public override MailType MailType => MailType.CustomCraft;
        protected override string TypeId => nameof(CustomCraftMail);

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        public CustomCraftMail(long blockIndex, Guid id, long requiredBlockIndex,
            Equipment equipment) : base(blockIndex, id, requiredBlockIndex)
        {
            Equipment = equipment;
        }

        public CustomCraftMail(Dictionary serialized) : base(serialized)
        {
            Equipment = new Equipment(serialized["equipment"]);
        }

        public override IValue Serialize() =>
            ((Dictionary)base.Serialize()).Add("equipment", Equipment.Serialize());
    }
}
