#nullable enable
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public interface IDelegationRepository
    {
        Address DelegateeAccountAddress { get; }

        Address DelegatorAccountAddress { get; }

        IWorld World { get; }

        IActionContext ActionContext { get; }

        IDelegatee GetDelegatee(Address address);

        IDelegator GetDelegator(Address address);

        DelegateeMetadata GetDelegateeMetadata(Address delegateeAddress);

        DelegatorMetadata GetDelegatorMetadata(Address delegatorAddress);

        Bond GetBond(IDelegatee delegatee, Address delegatorAddress);

        UnbondLockIn GetUnbondLockIn(IDelegatee delegatee, Address delegatorAddress);

        UnbondLockIn GetUnlimitedUnbondLockIn(Address address);

        RebondGrace GetRebondGrace(IDelegatee delegatee, Address delegatorAddress);

        RebondGrace GetUnlimitedRebondGrace(Address address);

        UnbondingSet GetUnbondingSet();

        RewardBase GetRewardBase(IDelegatee delegatee, long height);

        LumpSumRewardsRecord? GetLumpSumRewardsRecord(IDelegatee delegatee, long height);

        LumpSumRewardsRecord? GetCurrentLumpSumRewardsRecord(IDelegatee delegatee);

        FungibleAssetValue GetBalance(Address address, Currency currency);

        void SetDelegatee(IDelegatee delegatee);

        void SetDelegator(IDelegator delegator);

        void SetDelegateeMetadata(DelegateeMetadata delegateeMetadata);

        void SetDelegatorMetadata(DelegatorMetadata delegatorMetadata);

        void SetBond(Bond bond);

        void SetUnbondLockIn(UnbondLockIn unbondLockIn);

        void SetRebondGrace(RebondGrace rebondGrace);

        void SetUnbondingSet(UnbondingSet unbondingSet);

        void SetRewardBase(RewardBase rewardBase);

        void SetLumpSumRewardsRecord(LumpSumRewardsRecord lumpSumRewardsRecord);

        void TransferAsset(Address sender, Address recipient, FungibleAssetValue value);

        void UpdateWorld(IWorld world);
    }
}
