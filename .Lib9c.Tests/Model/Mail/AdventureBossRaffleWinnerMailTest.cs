namespace Lib9c.Tests.Model.Mail
{
    using System;
    using Bencodex.Types;
    using Lib9c.Model.Mail;
    using Libplanet.Types.Assets;
    using Xunit;

    public class AdventureBossRaffleWinnerMailTest
    {
#pragma warning disable CS0618
        // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
        private readonly Currency _currency = Currency.Legacy("CURRENCY", 18, null);
#pragma warning restore CS0618

        [Fact]
        public void Serialize()
        {
            var mail = new AdventureBossRaffleWinnerMail(1, Guid.NewGuid(), 2, 1, 5 * _currency);
            var serialized = (Dictionary)mail.Serialize();
            var deserialized = (AdventureBossRaffleWinnerMail)Lib9c.Model.Mail.Mail.Deserialize(serialized);

            Assert.Equal(1, deserialized.Season);
            Assert.Equal(5 * _currency, deserialized.Reward);
        }
    }
}
