namespace Nekoyume.BlockChain.Policy
{
    public sealed class StakeIntervalPolicy : VariableSubPolicy<(long RewardInterval, long LockupInterval)>
    {
        private StakeIntervalPolicy(long rewardInterval, long lockupInterval)
            : base((rewardInterval, lockupInterval))
        {
        }

        private StakeIntervalPolicy(
            StakeIntervalPolicy stakeRewardIntervalPolicy,
            SpannedSubPolicy<(long, long)> spannedSubPolicy)
            : base(stakeRewardIntervalPolicy, spannedSubPolicy)
        {
        }

        public static IVariableSubPolicy<(long, long)> Mainnet =>
            new StakeIntervalPolicy(50400, 201600);

        // Previewnet
        public static IVariableSubPolicy<(long, long)> Permant =>
            new StakeIntervalPolicy(7200, 28800);
    }
}
