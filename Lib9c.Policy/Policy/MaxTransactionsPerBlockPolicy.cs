namespace Nekoyume.Blockchain.Policy
{
    public sealed class MaxTransactionsPerBlockPolicy : VariableSubPolicy<int>
    {
        private MaxTransactionsPerBlockPolicy(int defaultValue)
            : base(defaultValue)
        {
        }

        private MaxTransactionsPerBlockPolicy(
            MaxTransactionsPerBlockPolicy maxTransactionsPerBlockPolicy,
            SpannedSubPolicy<int> spannedSubPolicy)
            : base(maxTransactionsPerBlockPolicy, spannedSubPolicy)
        {
        }

        public static IVariableSubPolicy<int> Default =>
            new MaxTransactionsPerBlockPolicy(int.MaxValue);

        public static IVariableSubPolicy<int> Odin =>
            Default
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 0,
                    value: BlockPolicySource.MaxTransactionsPerBlock));

        public static IVariableSubPolicy<int> Heimdall =>
            Default
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 0,
                    value: BlockPolicySource.MaxTransactionsPerBlock));

        public static IVariableSubPolicy<int> Thor =>
            Default
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 0,
                    value: BlockPolicySource.MaxTransactionsPerBlock));
    }
}
