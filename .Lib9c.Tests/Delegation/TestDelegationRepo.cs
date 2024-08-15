#nullable enable
namespace Nekoyume.Delegation
{
    using System;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;

    public class TestDelegationRepo : IDelegationRepository
    {
        private readonly Address bondAddress = Addresses.Bond;
        private readonly Address unbondLockInAddress = Addresses.UnbondLockIn;
        private readonly Address rebondGraceAddress = Addresses.RebondGrace;
        private readonly Address unbondingSetAddress = Addresses.UnbondingSet;
        private readonly Address lumpSumRewardsRecordAddress = Addresses.LumpSumRewardsRecord;

        private IWorld _world;
        private IActionContext _context;
        private IAccount _bond;
        private IAccount _unbondLockIn;
        private IAccount _rebondGrace;
        private IAccount _unbondingSet;
        private IAccount _lumpSumRewardsRecord;

        public TestDelegationRepo(IWorld world, IActionContext context)
        {
            _world = world;
            _context = context;
            _bond = world.GetAccount(bondAddress);
            _unbondLockIn = world.GetAccount(unbondLockInAddress);
            _rebondGrace = world.GetAccount(rebondGraceAddress);
            _unbondingSet = world.GetAccount(unbondingSetAddress);
            _lumpSumRewardsRecord = world.GetAccount(lumpSumRewardsRecordAddress);
        }

        public IWorld World => _world
            .SetAccount(bondAddress, _bond)
            .SetAccount(unbondLockInAddress, _unbondLockIn)
            .SetAccount(rebondGraceAddress, _rebondGrace)
            .SetAccount(unbondingSetAddress, _unbondingSet)
            .SetAccount(lumpSumRewardsRecordAddress, _lumpSumRewardsRecord);

        public Bond GetBond(IDelegatee delegatee, Address delegatorAddress)
        {
            Address address = delegatee.BondAddress(delegatorAddress);
            IValue? value = _bond.GetState(address);
            return value is IValue bencoded
                ? new Bond(address, bencoded)
                : new Bond(address);
        }

        public UnbondLockIn GetUnbondLockIn(IDelegatee delegatee, Address delegatorAddress)
        {
            Address address = delegatee.UnbondLockInAddress(delegatorAddress);
            IValue? value = _unbondLockIn.GetState(address);
            return value is IValue bencoded
                ? new UnbondLockIn(address, delegatee.MaxUnbondLockInEntries, bencoded, this)
                : new UnbondLockIn(address, delegatee.MaxUnbondLockInEntries, delegatee.DelegationPoolAddress, delegatorAddress, this);
        }

        public UnbondLockIn GetUnlimitedUnbondLockIn(Address address)
        {
            IValue? value = _unbondLockIn.GetState(address);
            return value is IValue bencoded
                ? new UnbondLockIn(address, int.MaxValue, bencoded, this)
                : throw new InvalidOperationException("UnbondLockIn not found.");
        }

        public RebondGrace GetRebondGrace(IDelegatee delegatee, Address delegatorAddress)
        {
            Address address = delegatee.RebondGraceAddress(delegatorAddress);
            IValue? value = _rebondGrace.GetState(address);
            return value is IValue bencoded
                ? new RebondGrace(address, delegatee.MaxRebondGraceEntries, bencoded, this)
                : new RebondGrace(address, delegatee.MaxRebondGraceEntries, this);
        }

        public RebondGrace GetUnlimitedRebondGrace(Address address)
        {
            IValue? value = _rebondGrace.GetState(address);
            return value is IValue bencoded
                ? new RebondGrace(address, int.MaxValue, bencoded, this)
                : throw new InvalidOperationException("RebondGrace not found.");
        }

        public UnbondingSet GetUnbondingSet()
            => _unbondingSet.GetState(UnbondingSet.Address) is IValue bencoded
                ? new UnbondingSet(bencoded, this)
                : new UnbondingSet(this);

        public LumpSumRewardsRecord? GetLumpSumRewardsRecord(IDelegatee delegatee, long height)
        {
            Address address = delegatee.LumpSumRewardsRecordAddress(height);
            IValue? value = _lumpSumRewardsRecord.GetState(address);
            return value is IValue bencoded
                ? new LumpSumRewardsRecord(address, bencoded)
                : null;
        }

        public LumpSumRewardsRecord? GetCurrentLumpSumRewardsRecord(IDelegatee delegatee)
        {
            Address address = delegatee.CurrentLumpSumRewardsRecordAddress();
            IValue? value = _lumpSumRewardsRecord.GetState(address);
            return value is IValue bencoded
                ? new LumpSumRewardsRecord(address, bencoded)
                : null;
        }

        public void SetBond(Bond bond)
        {
            _bond = bond.IsEmpty
                ? _bond.RemoveState(bond.Address)
                : _bond.SetState(bond.Address, bond.Bencoded);
        }

        public void SetUnbondLockIn(UnbondLockIn unbondLockIn)
        {
            _unbondLockIn = unbondLockIn.IsEmpty
                ? _unbondLockIn.RemoveState(unbondLockIn.Address)
                : _unbondLockIn.SetState(unbondLockIn.Address, unbondLockIn.Bencoded);
        }

        public void SetRebondGrace(RebondGrace rebondGrace)
        {
            _rebondGrace = rebondGrace.IsEmpty
                ? _rebondGrace.RemoveState(rebondGrace.Address)
                : _rebondGrace.SetState(rebondGrace.Address, rebondGrace.Bencoded);
        }

        public void SetUnbondingSet(UnbondingSet unbondingSet)
        {
            _unbondingSet = unbondingSet.IsEmpty
                ? _unbondingSet.RemoveState(UnbondingSet.Address)
                : _unbondingSet.SetState(UnbondingSet.Address, unbondingSet.Bencoded);
        }

        public void SetLumpSumRewardsRecord(LumpSumRewardsRecord lumpSumRewardsRecord)
        {
            _lumpSumRewardsRecord = _lumpSumRewardsRecord.SetState(
                lumpSumRewardsRecord.Address, lumpSumRewardsRecord.Bencoded);
        }

        public void TransferAsset(Address sender, Address recipient, FungibleAssetValue value)
            => _world = _world.TransferAsset(_context, sender, recipient, value);

        public void MintAsset(Address recipient, FungibleAssetValue value)
            => _world = _world.MintAsset(_context, recipient, value);
    }
}
