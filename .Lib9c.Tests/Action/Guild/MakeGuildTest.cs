namespace Lib9c.Tests.Action.Guild
{
    using Nekoyume.Action.Guild;
    using Nekoyume.Action.Loader;
    using Xunit;

    public class MakeGuildTest
    {
        [Fact]
        public void Serialization()
        {
            var action = new MakeGuild();
            var plainValue = action.PlainValue;

            var actionLoader = new NCActionLoader();
            var loadedRaw = actionLoader.LoadAction(0, plainValue);
            Assert.IsType<MakeGuild>(loadedRaw);
        }
    }
}
