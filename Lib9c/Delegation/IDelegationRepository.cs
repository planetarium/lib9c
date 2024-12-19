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

        /// <summary>
        /// Get the current <see cref="RewardBase"/> of the <paramref name="delegatee"/>.
        /// </summary>
        /// <param name="delegatee">
        /// The <see cref="IDelegatee"/> to get the current <see cref="RewardBase"/>.
        /// </param>
        /// <returns>
        /// The current <see cref="RewardBase"/> of the <paramref name="delegatee"/>.
        /// </returns>
        RewardBase? GetCurrentRewardBase(IDelegatee delegatee);

        /// <summary>
        /// Get the <see cref="RewardBase"/> of the <paramref name="delegatee"/>
        /// at the given <paramref name="height"/>.
        /// </summary>
        /// <param name="delegatee">
        /// The <see cref="IDelegatee"/> to get the <see cref="RewardBase"/> of.
        /// </param>
        /// <param name="height">
        /// The height to get the <see cref="RewardBase"/> at.
        /// </param>
        /// <returns>
        /// The <see cref="RewardBase"/> of the <paramref name="delegatee"/>
        /// at the given <paramref name="height"/>.
        /// </returns>
        RewardBase? GetRewardBase(IDelegatee delegatee, long height);

        LumpSumRewardsRecord? GetCurrentLumpSumRewardsRecord(IDelegatee delegatee);

        LumpSumRewardsRecord? GetLumpSumRewardsRecord(IDelegatee delegatee, long height);

        FungibleAssetValue GetBalance(Address address, Currency currency);

        void SetDelegatee(IDelegatee delegatee);

        void SetDelegator(IDelegator delegator);

        void SetDelegateeMetadata(DelegateeMetadata delegateeMetadata);

        void SetDelegatorMetadata(DelegatorMetadata delegatorMetadata);

        void SetBond(Bond bond);

        void SetUnbondLockIn(UnbondLockIn unbondLockIn);

        void SetRebondGrace(RebondGrace rebondGrace);

        void SetUnbondingSet(UnbondingSet unbondingSet);

        /// <summary>
        /// Set the <see cref="RewardBase"/> of the <see cref="IDelegatee"/>.
        /// </summary>
        /// <param name="rewardBase">
        /// The <see cref="RewardBase"/> to set.
        /// </param>
        void SetRewardBase(RewardBase rewardBase);

        void SetLumpSumRewardsRecord(LumpSumRewardsRecord lumpSumRewardsRecord);

        /// <summary>
        /// Remove the <see cref="LumpSumRewardsRecord"/> from the <see cref="IDelegatee"/>.
        /// This is used when the <see cref="LumpSumRewardsRecord"/> is no longer needed.
        /// This can be removed when the migration for <see cref="RewardBase"/> is done.
        /// </summary>
        /// <param name="lumpSumRewardsRecord">
        /// The <see cref="LumpSumRewardsRecord"/> to remove.
        /// </param>
        void RemoveLumpSumRewardsRecord(LumpSumRewardsRecord lumpSumRewardsRecord);

        void TransferAsset(Address sender, Address recipient, FungibleAssetValue value);

        void UpdateWorld(IWorld world);
    }
}
