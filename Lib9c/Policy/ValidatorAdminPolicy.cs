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
            "0000000000000000000000000000000000000000000000000000000000000001");

        public static readonly PrivateKey TestValidatorAdminKey = new PrivateKey(
            "0000000000000000000000000000000000000000000000000000000000000002");

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
                    value: BlockPolicySource.ValidatorAdmin));

        public static IVariableSubPolicy<PublicKey> Permanent =>
            Default
                .Add(new SpannedSubPolicy<PublicKey>(
                    startIndex: 0,
                    endIndex: null,
                    filter: null,
                    value: BlockPolicySource.ValidatorAdmin));

        public static IVariableSubPolicy<PublicKey> Test =>
            new ValidatorAdminPolicy(TestValidatorAdminKey.PublicKey);
    }
}
