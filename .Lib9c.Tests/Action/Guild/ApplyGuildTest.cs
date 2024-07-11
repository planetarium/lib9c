namespace Lib9c.Tests.Action.Guild
{
    using Libplanet.Crypto;
    using Nekoyume.Action.Guild;
    using Nekoyume.Action.Loader;
    using Nekoyume.TypedAddress;
    using Xunit;

    public class ApplyGuildTest
    {
        [Fact]
        public void Serialization()
        {
            var guildAddress = new GuildAddress(new PrivateKey().Address);
            var action = new ApplyGuild(guildAddress);
            var plainValue = action.PlainValue;

            var actionLoader = new NCActionLoader();
            var loadedRaw = actionLoader.LoadAction(0, plainValue);
            var loadedAction = Assert.IsType<ApplyGuild>(loadedRaw);
            Assert.Equal(guildAddress, loadedAction.GuildAddress);
        }
    }
}
