namespace Lib9c.Tests.Util
{
    using Libplanet.Crypto;
    using Nekoyume.TypedAddress;

    public static class AddressUtil
    {
        public static AgentAddress CreateAgentAddress()
        {
            return new AgentAddress(new PrivateKey().Address);
        }

        public static GuildAddress CreateGuildAddress()
        {
            return new GuildAddress(new PrivateKey().Address);
        }
    }
}
