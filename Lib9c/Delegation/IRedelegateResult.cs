namespace Nekoyume.Delegation
{
    public interface IRedelegateResult
    {
        IDelegatee SrcDelegatee { get; }

        IDelegatee DstDelegatee { get; }

        Bond SrcBond { get; }

        Bond DstBond { get; }

        RebondGrace RebondGrace { get; }

        UnbondingSet UnbondingSet { get; }
    }
}
