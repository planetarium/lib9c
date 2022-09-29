using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet;
using Libplanet.Crypto;

namespace Nekoyume.BlockChain.Policy
{
    public sealed class ValidatorsPolicy : VariableSubPolicy<IEnumerable<PublicKey>>
    {
        private ValidatorsPolicy(IEnumerable<PublicKey> defaultValue)
            : base(defaultValue)
        {
        }

        private ValidatorsPolicy(
            ValidatorsPolicy validatorsPolicy,
            SpannedSubPolicy<IEnumerable<PublicKey>> spannedSubPolicy)
            : base(validatorsPolicy, spannedSubPolicy)
        {
        }

        public static IVariableSubPolicy<IEnumerable<PublicKey>> Default =>
            new ValidatorsPolicy(ImmutableArray<PublicKey>.Empty);

        public static IVariableSubPolicy<IEnumerable<PublicKey>> Mainnet =>
            Default
                .Add(new SpannedSubPolicy<IEnumerable<PublicKey>>(
                    startIndex: 0,
                    endIndex: null,
                    filter: null,
                    value: BlockPolicySource.Validators));

         public static IVariableSubPolicy<IEnumerable<PublicKey>> Permanent =>
             Default
                 .Add(new SpannedSubPolicy<IEnumerable<PublicKey>>(
                     startIndex: 0,
                     endIndex: null,
                     filter: null,
                     value: BlockPolicySource.Validators));
    }
}
