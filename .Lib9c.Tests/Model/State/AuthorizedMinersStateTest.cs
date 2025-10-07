namespace Lib9c.Tests.Model.State
{
    using Bencodex.Types;
    using Lib9c.Model.State;
    using Libplanet.Crypto;
    using Xunit;

    public class AuthorizedMinersStateTest
    {
        [Fact]
        public void Serialize()
        {
            var miners = GetNewMiners();
            var state = new AuthorizedMinersState(
                miners,
                50,
                1000
            );

            var serialized = (Dictionary)state.Serialize();
            var deserialized = new AuthorizedMinersState(serialized);

            Assert.Equal(miners, deserialized.Miners);
            Assert.Equal(50, deserialized.Interval);
            Assert.Equal(1000, deserialized.ValidUntil);
        }

        private static Address[] GetNewMiners()
        {
            return new[]
            {
                new Address(
                    new byte[]
                    {
                        0x01, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                    }
                ),
                new Address(
                    new byte[]
                    {
                        0x02, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00,
                    }
                ),
            };
        }
    }
}
