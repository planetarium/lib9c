using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Nekoyume.BlockChain.Policy
{
    public sealed class MaxBlockBytesPolicy : VariableSubPolicy<long>
    {
        private MaxBlockBytesPolicy(long defaultValue)
            : base(defaultValue)
        {
        }

        private MaxBlockBytesPolicy(
            MaxBlockBytesPolicy maxBlockBytesPolicy,
            SpannedSubPolicy<long> spannedSubPolicy)
            : base(maxBlockBytesPolicy, spannedSubPolicy)
        {
        }

        public static IVariableSubPolicy<long> Default =>
            new MaxBlockBytesPolicy(long.MaxValue);

        public static IVariableSubPolicy<long> Mainnet =>
            Default
                // Note: The genesis block of 9c-main weighs 11,085,640 B (11 MiB).
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 0L,
                    value: 1024L * 1024L * 15L))   // 15 MiB
                // Note: Initial analysis of the heaviest block of 9c-main
                // (except for the genesis) weighs 58,408 B (58 KiB).
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 1L,
                    value: 1024L * 100L))         // 100 KiB
                // Note: Temporary limit increase for resolving
                // https://github.com/planetarium/NineChronicles/issues/777.
                // Issued for v100081.  Temporary ad hoc increase was introduced
                // around 2_500_000.
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 2_000_001L,
                    value: 1024L * 1024L * 10L))    // 10 MiB
                // Note: Reverting back to the previous limit.  Issued for v100086.
                // FIXME: Starting index must be finalized accordingly before deployment.
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 2_800_001L,
                    value: 1024L * 100L));        // 100 KiB

        // Note: For internal testing.
        public static IVariableSubPolicy<long> Internal =>
            Default
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 0L,
                    value: 1024L * 1024L * 15L))   // 15 MiB
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 1L,
                    value: 1024L * 100L))         // 100 KiB
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 2_000_001L,
                    value: 1024L * 1024L * 10L))    // 10 MiB
                .Add(new SpannedSubPolicy<long>(
                    startIndex: 2_800_001,
                    value: 1024L * 100L));        // 100 KiB
    }
}
