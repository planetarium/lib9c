#nullable enable
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Delegation
{
    public class DelegationRepository : IDelegationRepository
    {
        private readonly Address BondAddress = Addresses.Bond;
        private readonly Address UnbondLockInAddress = Addresses.UnbondLockIn;
        private readonly Address RebondGraceAddress = Addresses.RebondGrace;
        private readonly Address UnbondingSetAddress = Addresses.UnbondingSet;
        private readonly Address LumpSumRewardsRecordAddress = Addresses.LumpSumRewardsRecord;

        private IWorld _world;
        private IActionContext _context;
        private IAccount _bond;
        private IAccount _unbondLockIn;
        private IAccount _rebondGrace;
        private IAccount _unbondingSet;
        private IAccount _lumpSumRewardsRecord;

        public DelegationRepository(IWorld world, IActionContext context)
        {
            _world = world;
            _context = context;
            _bond = world.GetAccount(BondAddress);
            _unbondLockIn = world.GetAccount(UnbondLockInAddress);
            _rebondGrace = world.GetAccount(RebondGraceAddress);
            _unbondingSet = world.GetAccount(UnbondingSetAddress);
            _lumpSumRewardsRecord = world.GetAccount(LumpSumRewardsRecordAddress);
        }

        public IWorld World => _world
            .SetAccount(BondAddress, _bond)
            .SetAccount(UnbondLockInAddress, _unbondLockIn)
            .SetAccount(RebondGraceAddress, _rebondGrace)
            .SetAccount(UnbondingSetAddress, _unbondingSet)
            .SetAccount(LumpSumRewardsRecordAddress, _lumpSumRewardsRecord);

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
                ? new UnbondLockIn(address, delegatee.MaxUnbondLockInEntries, bencoded)
                : new UnbondLockIn(address, delegatee.MaxUnbondLockInEntries);
        }

        public RebondGrace GetRebondGrace(IDelegatee delegatee, Address delegatorAddress)
        {
            Address address = delegatee.RebondGraceAddress(delegatorAddress);
            IValue? value = _rebondGrace.GetState(address);
            return value is IValue bencoded
                ? new RebondGrace(address, delegatee.MaxRebondGraceEntries, bencoded)
                : new RebondGrace(address, delegatee.MaxUnbondLockInEntries);
        }

        public UnbondingSet GetUnbondingSet()
            => _unbondingSet.GetState(UnbondingSet.Address) is IValue bencoded
                ? new UnbondingSet(bencoded)
                : new UnbondingSet();

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
    }
}
