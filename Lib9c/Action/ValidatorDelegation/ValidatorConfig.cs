using Libplanet.Crypto;

namespace Nekoyume.Action.ValidatorDelegation
{
    public static class ValidatorConfig
    {
        public static readonly Address PlanetariumValidatorAddress =
            new Address("0x8E1b572db70aB80bb02783A0D2c594A0edE6db28");

        public static readonly PublicKey PlanetariumValidatorPublicKey =
            PublicKey.FromHex("03a0f95711564d10c60ba1889d068c26cb8e5fcd5211d5aeb8810e133d629aa306");
    }
}
