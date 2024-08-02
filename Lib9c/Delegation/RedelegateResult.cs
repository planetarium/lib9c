namespace Nekoyume.Delegation
{
    public class RedelegateResult
    {
        public RedelegateResult(
            IDelegatee srcDelegatee,
            IDelegatee dstDelegatee,
            Bond srcBond,
            Bond dstBond,
            RebondGrace rebondGrace)
        {
            SrcDelegatee = srcDelegatee;
            DstDelegatee = dstDelegatee;
            SrcBond = srcBond;
            DstBond = dstBond;
            RebondGrace = rebondGrace;
        }

        public IDelegatee SrcDelegatee { get; }

        public IDelegatee DstDelegatee { get; }

        public Bond SrcBond { get; }

        public Bond DstBond { get; }

        public RebondGrace RebondGrace { get; }
    }
}
