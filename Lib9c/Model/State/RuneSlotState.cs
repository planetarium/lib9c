using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Rune;
using Nekoyume.TableData;

namespace Nekoyume.Model.State
{
    public class RuneSlotState : IState
    {
        public static Address DeriveAddress(Address avatarAddress, BattleType battleType) =>
            avatarAddress.Derive($"rune_slot_state_{battleType}");

        public BattleType BattleType { get; }

        private readonly List<RuneSlot> _slots = new List<RuneSlot>();

        public RuneSlotState(BattleType battleType)
        {
            BattleType = battleType;
            _slots.Add(new RuneSlot(0, RuneSlotType.Default, RuneType.Stat, false));
            _slots.Add(new RuneSlot(1, RuneSlotType.Ncg, RuneType.Stat, true));
            _slots.Add(new RuneSlot(2, RuneSlotType.Stake, RuneType.Stat, true));
            _slots.Add(new RuneSlot(3, RuneSlotType.Default, RuneType.Skill, false));
            _slots.Add(new RuneSlot(4, RuneSlotType.Ncg, RuneType.Skill, true));
            _slots.Add(new RuneSlot(5, RuneSlotType.Stake, RuneType.Skill,true));
        }

        public RuneSlotState(List serialized)
        {
            BattleType = serialized[0].ToEnum<BattleType>();
            _slots = ((List)serialized[1]).Select(x => new RuneSlot((List)x)).ToList();
        }

        public IValue Serialize()
        {
            var result = List.Empty
                .Add(BattleType.Serialize())
                .Add(new List(_slots.Select(x => x.Serialize())));
            return result;
        }

        public void UpdateSlot(
            List<RuneSlotInfo> runeInfos,
            List<RuneState> runeStates,
            RuneListSheet runeListSheet)
        {
            foreach (var slot in _slots)
            {
                var runeInfo = runeInfos.FirstOrDefault(x => x.SlotIndex == slot.Index);
                if (runeInfo is null)
                {
                    slot.Unequip();
                }
                else
                {
                    if (IsUsableSlot(runeStates, runeListSheet, slot, runeInfo, out var runeState))
                    {
                        slot.Equip(runeState);
                    }
                    else
                    {
                        throw new IsUsableSlotException(
                            $"[{nameof(RuneSlotState)}] Index : {slot.Index}");
                    }
                }
            }
        }

        public void UpdateSlotItem(RuneState runeState)
        {
            foreach (var slot in _slots)
            {
                if (!slot.IsEquipped(out var state))
                {
                    continue;
                }

                if (state.RuneId != runeState.RuneId)
                {
                    continue;
                }

                slot.Equip(runeState);
                return;
            }
        }

        private bool IsUsableSlot(
            IEnumerable<RuneState> runeStates,
            RuneListSheet runeListSheet,
            RuneSlot slot,
            RuneSlotInfo runeInfo,
            out RuneState runeState)
        {
            if (slot.IsLock)
            {
                throw new SlotIsLockedException(
                    $"[{nameof(RuneSlotState)}] Index : {slot.Index}");
            }

            var runeId = runeInfo.RuneId;
            if (!runeListSheet.TryGetValue(runeId, out var row))
            {
                throw new RuneListNotFoundException(
                    $"[{nameof(RuneSlotState)}] Index : {slot.Index} / runeId : {runeId}");
            }

            var runeType = (RuneType)row.RuneType;
            if (slot.RuneType != runeType)
            {
                throw new SlotRuneTypeException(
                    $"[{nameof(RuneSlotState)}] Index : {slot.Index} / {slot.RuneType} != {runeType}");
            }

            var runePlace = (RuneUsePlace)row.UsePlace;
            if (!BattleType.IsEquippableRune(runePlace))
            {
                throw new IsEquippableRuneException(
                    $"[{nameof(RuneSlotState)}] Index : {slot.Index} / runePlace : {runePlace}");
            }

            runeState = runeStates.FirstOrDefault(x => x.RuneId == runeId);
            if (runeState is null)
            {
                throw new RuneStateNotFoundException(
                    $"[{nameof(RuneSlotState)}] Index : {slot.Index} / runeId : {runeId}");
            }

            return true;
        }

        public void Unlock(int index)
        {
            var slot = _slots.FirstOrDefault(x => x.Index == index);
            if (slot is null)
            {
                throw new SlotNotFoundException(
                    $"[{nameof(RuneSlotState)}] Index : {index}");
            }

            if (!slot.IsLock)
            {
                throw new SlotIsAlreadyUnlockedException(
                    $"[{nameof(RuneSlotState)}] Index : {index}");
            }

            slot.Unlock();
        }

        public List<RuneSlot> GetRuneSlot()
        {
            return _slots;
        }

        public List<RuneState> GetEquippedRuneStates()
        {
            var result = new List<RuneState>();
            foreach (var slot in _slots.Where(slot => !slot.IsLock))
            {
                if (slot.IsEquipped(out var runeState))
                {
                    result.Add(runeState);
                }
            }

            return result;
        }

        public List<RuneSlotInfo> GetEquippedRuneSlotInfos()
        {
            var result = new List<RuneSlotInfo>();
            foreach (var slot in _slots.Where(slot => !slot.IsLock))
            {
                if (slot.IsEquipped(out var runeState))
                {
                    result.Add(new RuneSlotInfo(slot.Index, runeState.RuneId, runeState.Level));
                }
            }

            return result;
        }

        public List<RuneOptionSheet.Row.RuneOptionInfo> GetEquippedRuneOptions(RuneOptionSheet sheet)
        {
            var result = new List<RuneOptionSheet.Row.RuneOptionInfo>();
            foreach (var runeState in GetEquippedRuneStates())
            {
                if (!sheet.TryGetValue(runeState.RuneId, out var row))
                {
                    continue;
                }

                if (!row.LevelOptionMap.TryGetValue(runeState.Level, out var statInfo))
                {
                    continue;
                }

                result.Add(statInfo);
            }

            return result;
        }
    }
}
