namespace Nekoyume.Delegation
{
    public class UndelegateResult
    {
        public UndelegateResult(
            IDelegatee delegatee,
            Bond bond,
            UnbondLockIn unbondLockIn)
        {
            Delegatee = delegatee;
            Bond = bond;
            UnbondLockIn = unbondLockIn;
        }

        public IDelegatee Delegatee { get; }

        public Bond Bond { get; }

        public UnbondLockIn UnbondLockIn { get; }
    }
}
