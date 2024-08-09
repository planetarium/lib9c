#nullable enable
namespace Nekoyume.Delegation
{
    public class UndelegateResult<T> : IUndelegateResult
        where T : IDelegatee
    {
        public UndelegateResult(
            T delegatee,
            Bond bond,
            UnbondLockIn unbondLockIn,
            UnbondingSet unbondingSet,
            LumpSumRewardsRecord lumpSumRewardsRecord)
        {
            Delegatee = delegatee;
            Bond = bond;
            UnbondLockIn = unbondLockIn;
            UnbondingSet = unbondingSet;
            LumpSumRewardsRecord = lumpSumRewardsRecord;
        }

        public T Delegatee { get; }

        IDelegatee IUndelegateResult.Delegatee => Delegatee;

        public Bond Bond { get; }

        public UnbondLockIn UnbondLockIn { get; }

        public UnbondingSet UnbondingSet { get; }

        public LumpSumRewardsRecord LumpSumRewardsRecord { get; }
    }
}
