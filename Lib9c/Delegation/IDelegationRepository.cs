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

        UnbondLockIn GetUnlimitedUnbondLockIn(Address address);

        RebondGrace GetRebondGrace(IDelegatee delegatee, Address delegatorAddress);

        RebondGrace GetUnlimitedRebondGrace(Address address);

        UnbondingSet GetUnbondingSet();

        LumpSumRewardsRecord? GetLumpSumRewardsRecord(IDelegatee delegatee, long height);

        LumpSumRewardsRecord? GetCurrentLumpSumRewardsRecord(IDelegatee delegatee);

        void SetBond(Bond bond);

        void SetUnbondLockIn(UnbondLockIn unbondLockIn);

        void SetRebondGrace(RebondGrace rebondGrace);

        void SetUnbondingSet(UnbondingSet unbondingSet);

        void SetLumpSumRewardsRecord(LumpSumRewardsRecord lumpSumRewardsRecord);

        void Reward(IDelegatee delegatee, long height, FungibleAssetValue reward);

        void TransferAsset(Address sender, Address recipient, FungibleAssetValue value);
    }
}
