namespace Lib9c.Tests.Model.Mail
{
    using System;
    using System.Linq;
    using Nekoyume.Action;
    using Nekoyume.Model.Mail;
    using Xunit;

    public class MailBoxTest
    {
        [Fact]
        public void Serialize()
        {
            var mailBox = new MailBox();
            var serialized = (Bencodex.Types.List)mailBox.Serialize();
            var deserialized = new MailBox(serialized);

            Assert.Equal(mailBox, deserialized);
        }

        [Theory]
        [InlineData(MailBox.MaxCount * 2)]
        [InlineData(MailBox.MaxCount * 2, 0, MailBox.MaxCount - 1, MailBox.MaxCount * 2 - 1)]
        public void CleanUp(int addCount, params int[] indexesThatShouldRemain)
        {
            var mailBox = new MailBox();
            for (var i = 0; i < addCount; i++)
            {
                mailBox.Add(new CombinationMail(
                    new CombinationConsumable5.ResultModel(), i / 2, Guid.NewGuid(), i + 1));
            }

            var mailIdsThatShouldRemain = indexesThatShouldRemain
                .Select(e => mailBox[e].id)
                .ToArray();

            mailBox.CleanUp(mailIdsThatShouldRemain);
            Assert.Equal(MailBox.MaxCount, mailBox.Count);

            var lastBlockIndex = long.MaxValue;
            Guid? lastGuid = null;
            foreach (var mail in mailBox)
            {
                if (mail.blockIndex == lastBlockIndex)
                {
                    if (lastGuid.HasValue)
                    {
                        Assert.Equal(1, mail.id.CompareTo(lastGuid.Value));
                    }

                    lastBlockIndex = mail.blockIndex;
                    lastGuid = mail.id;
                    continue;
                }

                Assert.True(mail.blockIndex < lastBlockIndex);
                lastBlockIndex = mail.blockIndex;
                lastGuid = mail.id;
            }

            foreach (var mailId in mailIdsThatShouldRemain)
            {
                Assert.Contains(mailBox, mail => mail.id.Equals(mailId));
            }
        }

        [Theory]
        [InlineData(MailBox.MaxCount * 2, 0, false)]
        [InlineData(MailBox.MaxCount * 2, MailBox.MaxCount, false)]
        [InlineData(MailBox.MaxCount * 2, MailBox.MaxCount + 1, true)]
        public void CleanUp_Throw_ArgumentOutOfRangeException(int addCount, int shouldRemainCount, bool throwException)
        {
            var mailBox = new MailBox();
            for (var i = 0; i < addCount; i++)
            {
                mailBox.Add(new CombinationMail(
                    new CombinationConsumable5.ResultModel(), i / 2, Guid.NewGuid(), i + 1));
            }

            var mailIdsThatShouldRemain = mailBox
                .Take(shouldRemainCount)
                .Select(e => e.id)
                .ToArray();

            if (throwException)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => mailBox.CleanUp(mailIdsThatShouldRemain));
                Assert.Equal(addCount, mailBox.Count);
            }
            else
            {
                mailBox.CleanUp(mailIdsThatShouldRemain);
                Assert.Equal(MailBox.MaxCount, mailBox.Count);
            }
        }
    }
}
