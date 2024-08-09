#nullable enable
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
            UnbondingSet unbondingSet,
            LumpSumRewardsRecord srcLumpSumRewardsRecord,
            LumpSumRewardsRecord dstLumpSumRewardsRecord)
        {
            SrcDelegatee = srcDelegatee;
            DstDelegatee = dstDelegatee;
            SrcBond = srcBond;
            DstBond = dstBond;
            RebondGrace = rebondGrace;
            UnbondingSet = unbondingSet;
            SrcLumpSumRewardsRecord = srcLumpSumRewardsRecord;
            DstLumpSumRewardsRecord = dstLumpSumRewardsRecord;
        }

        public T SrcDelegatee { get; }

        IDelegatee IRedelegateResult.SrcDelegatee => SrcDelegatee;

        public T DstDelegatee { get; }

        IDelegatee IRedelegateResult.DstDelegatee => DstDelegatee;

        public Bond SrcBond { get; }

        public Bond DstBond { get; }

        public RebondGrace RebondGrace { get; }

        public UnbondingSet UnbondingSet { get; }

        public LumpSumRewardsRecord SrcLumpSumRewardsRecord { get; }

        public LumpSumRewardsRecord DstLumpSumRewardsRecord { get; }
    }
}
