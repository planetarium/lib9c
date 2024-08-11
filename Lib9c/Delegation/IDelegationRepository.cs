#nullable enable
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public interface IDelegationRepository
    {
        IWorld World { get; }

        Bond GetBond(IDelegatee delegatee, Address delegatorAddress);

        UnbondLockIn GetUnbondLockIn(IDelegatee delegatee, Address delegatorAddress);

        RebondGrace GetRebondGrace(IDelegatee delegatee, Address delegatorAddress);

        UnbondingSet GetUnbondingSet();

        LumpSumRewardsRecord? GetLumpSumRewardsRecord(IDelegatee delegatee, long height);

        LumpSumRewardsRecord? GetCurrentLumpSumRewardsRecord(IDelegatee delegatee);

        void SetBond(Bond bond);

        void SetUnbondLockIn(UnbondLockIn unbondLockIn);

        void SetRebondGrace(RebondGrace rebondGrace);

        void SetUnbondingSet(UnbondingSet unbondingSet);

        void SetLumpSumRewardsRecord(LumpSumRewardsRecord lumpSumRewardsRecord);

        void TransferAsset(Address sender, Address recipient, FungibleAssetValue value);
    }
}
