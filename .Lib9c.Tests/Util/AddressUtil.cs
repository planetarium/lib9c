namespace Lib9c.Tests.Util
{
    using Lib9c.TypedAddress;
    using Libplanet.Crypto;

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
