using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet;
using Libplanet.Consensus;
using Libplanet.Crypto;

namespace Nekoyume.BlockChain.Policy
{
    public sealed class ValidatorAdminPolicy : VariableSubPolicy<PublicKey>
    {
        public static readonly PrivateKey DefaultValidatorAdminKey = new PrivateKey(
            "1d3d794aff817f1d170e67ea20f7ce8f718081fbb181e7d550980d0b686a607d");

        public static readonly PrivateKey TestValidatorAdminKey = new PrivateKey(
            "79ab752e92df361eb9af5a3fa024e358a0ad9324fcdee5fd2bf1debaec455623");

        public static readonly PublicKey MainnetValidatorAdmin = new PublicKey(
            ByteUtil.ParseHex("0326e7f518eadfb1addc320755eeb78e385cf4b9d56986677a092a708c86990ae1"));

        private ValidatorAdminPolicy(PublicKey defaultValue)
            : base(defaultValue)
        {
        }

        private ValidatorAdminPolicy(
            ValidatorAdminPolicy validatorAdminPolicy,
            SpannedSubPolicy<PublicKey> spannedSubPolicy)
            : base(validatorAdminPolicy, spannedSubPolicy)
        {
        }

        public static IVariableSubPolicy<PublicKey> Default =>
            new ValidatorAdminPolicy(DefaultValidatorAdminKey.PublicKey);

        public static IVariableSubPolicy<PublicKey> Mainnet =>
            Default
                .Add(new SpannedSubPolicy<PublicKey>(
                    startIndex: 0,
                    endIndex: null,
                    filter: null,
                    value: MainnetValidatorAdmin));

        public static IVariableSubPolicy<PublicKey> Internal =>
            Default
                .Add(new SpannedSubPolicy<PublicKey>(
                    startIndex: 0,
                    endIndex: null,
                    filter: null,
                    value: MainnetValidatorAdmin));

        public static IVariableSubPolicy<PublicKey> Permanent =>
            Default
                .Add(new SpannedSubPolicy<PublicKey>(
                    startIndex: 0,
                    endIndex: null,
                    filter: null,
                    value: MainnetValidatorAdmin));

        public static IVariableSubPolicy<PublicKey> Test =>
            new ValidatorAdminPolicy(TestValidatorAdminKey.PublicKey);
    }
}
