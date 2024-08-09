#nullable enable
namespace Nekoyume.Delegation
{
    public interface IUndelegateResult
    {
        IDelegatee Delegatee { get; }

        Bond Bond { get; }

        UnbondLockIn UnbondLockIn { get; }

        UnbondingSet UnbondingSet { get; }

        LumpSumRewardsRecord LumpSumRewardsRecord { get; }
    }
}
