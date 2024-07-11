namespace Lib9c.Tests.Action.Guild
{
    using Nekoyume.Action.Guild;
    using Nekoyume.Action.Loader;
    using Xunit;

    public class QuitGuildTest
    {
        [Fact]
        public void Serialization()
        {
            var action = new QuitGuild();
            var plainValue = action.PlainValue;

            var actionLoader = new NCActionLoader();
            var loadedRaw = actionLoader.LoadAction(0, plainValue);
            Assert.IsType<QuitGuild>(loadedRaw);
        }
    }
}
