namespace Nekoyume.Blockchain.Policy
{
    public sealed class MaxTransactionsPerSignerPerBlockPolicy : VariableSubPolicy<int>
    {
        private MaxTransactionsPerSignerPerBlockPolicy(int defaultValue)
            : base(defaultValue)
        {
        }

        private MaxTransactionsPerSignerPerBlockPolicy(
            MaxTransactionsPerSignerPerBlockPolicy maxTransactionsPerSignerPerBlockPolicy,
            SpannedSubPolicy<int> spannedSubPolicy)
            : base(maxTransactionsPerSignerPerBlockPolicy, spannedSubPolicy)
        {
        }

        public static IVariableSubPolicy<int> Default =>
            new MaxTransactionsPerSignerPerBlockPolicy(int.MaxValue);

        public static IVariableSubPolicy<int> Odin =>
            Default;

        public static IVariableSubPolicy<int> Heimdall =>
            Default;

        public static IVariableSubPolicy<int> Thor =>
            Default;

        // Note: For internal testing.
        public static IVariableSubPolicy<int> OdinInternal =>
            Default;

        public static IVariableSubPolicy<int> HeimdallInternal =>
            Default;

        public static IVariableSubPolicy<int> ThorInternal =>
            Default;
    }
}
