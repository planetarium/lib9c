namespace Lib9c.Tests.Model.Mail
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.TableData;
    using Xunit;
    using Libplanet.Types.Assets;

    public class CustomCraftMailTest
    {
#pragma warning disable CS0618
        // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
        private readonly Currency _currency = Currency.Legacy("CRYSTAL", 18, null);
#pragma warning restore CS0618

        [Fact]
        public void Serialize()
        {
            var row = new EquipmentItemSheet.Row();
            row.Set(new List<string>
                { "20151000", "Weapon", "6", "Normal", "0", "ATK", "10", "0", "20160000", "0", });
            var equipment = (Equipment)ItemFactory.CreateItemUsable(row, Guid.NewGuid(), 1L);
            var mail = new CustomCraftMail(1, Guid.NewGuid(), 2, equipment);
            var serialized = (Dictionary)mail.Serialize();
            var deserialized = (CustomCraftMail)Nekoyume.Model.Mail.Mail.Deserialize(serialized);

            Assert.Equal(1, deserialized.blockIndex);
            Assert.Equal(2, deserialized.requiredBlockIndex);
            var eq = deserialized.Equipment;
            Assert.Equal(ItemSubType.Weapon, eq.ItemSubType);
            Assert.Equal(20151000, eq.IconId);
        }
    }
}
