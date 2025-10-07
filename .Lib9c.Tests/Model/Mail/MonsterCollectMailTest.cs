namespace Lib9c.Tests.Model.Mail
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Lib9c.Model.Mail;
    using Lib9c.Model.State;
    using Libplanet.Crypto;
    using Xunit;

    public class MonsterCollectMailTest
    {
        private readonly TableSheets _tableSheets;

        public MonsterCollectMailTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void Serialize()
        {
            var guid = new Guid("F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4");
            var address = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            var rewards = _tableSheets.MonsterCollectionRewardSheet.First!.Rewards;
            Assert.Equal(2, rewards.Count);

            var result = new MonsterCollectionResult(guid, address, rewards);
            var mail = new MonsterCollectionMail(result, 1, guid, 2);
            var serialized = (Dictionary)mail.Serialize();
            var deserialized = new MonsterCollectionMail(serialized);

            Assert.Equal(1, deserialized.blockIndex);
            Assert.Equal(2, deserialized.requiredBlockIndex);
            Assert.Equal(guid, deserialized.id);
            var attachment = (MonsterCollectionResult)deserialized.attachment;
            Assert.Equal(2, attachment.rewards.Count);
            Assert.Equal(rewards.First(), attachment.rewards.First());
        }

        [Fact]
        public void Serialize_DotNet_API()
        {
            var guid = new Guid("F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4");
            var address = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            var rewards = _tableSheets.MonsterCollectionRewardSheet.First!.Rewards;
            Assert.Equal(2, rewards.Count);

            var result = new MonsterCollectionResult(guid, address, rewards);
            var mail = new MonsterCollectionMail(result, 1, guid, 2);
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, mail);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (MonsterCollectionMail)formatter.Deserialize(ms);

            Assert.Equal(1, deserialized.blockIndex);
            Assert.Equal(2, deserialized.requiredBlockIndex);
            Assert.Equal(guid, deserialized.id);
            var attachment = (MonsterCollectionResult)deserialized.attachment;
            Assert.Equal(2, attachment.rewards.Count);
            Assert.Equal(rewards.First(), attachment.rewards.First());
        }
    }
}
