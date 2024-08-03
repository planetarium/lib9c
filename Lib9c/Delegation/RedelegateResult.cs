namespace Nekoyume.Delegation
{
    public class RedelegateResult<T> : IRedelegateResult
        where T : IDelegatee
    {
        public RedelegateResult(
            T srcDelegatee,
            T dstDelegatee,
            Bond srcBond,
            Bond dstBond,
            RebondGrace rebondGrace,
            UnbondingSet unbondingSet)
        {
            SrcDelegatee = srcDelegatee;
            DstDelegatee = dstDelegatee;
            SrcBond = srcBond;
            DstBond = dstBond;
            RebondGrace = rebondGrace;
            UnbondingSet = unbondingSet;
        }

        IDelegatee IRedelegateResult.SrcDelegatee => SrcDelegatee;

        IDelegatee IRedelegateResult.DstDelegatee => DstDelegatee;

        public T SrcDelegatee { get; }

        public T DstDelegatee { get; }

        public Bond SrcBond { get; }

        public Bond DstBond { get; }

        public RebondGrace RebondGrace { get; }

        public UnbondingSet UnbondingSet { get; }
    }
}
