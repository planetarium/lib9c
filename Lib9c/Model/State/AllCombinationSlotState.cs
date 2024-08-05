#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Module.CombinationSlot;
using UnityEngine;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// This is new version of combination slot State. This state stores all combinationSlot states of an avatar.
    /// AllCombinationSlotState has all combinationSlotStates as dictionary and has methods to get/set/update each combinationSlotState.
    /// Use this with <see cref="Nekoyume.Module.CombinationSlotStateModule"/>.
    /// </summary>
    public class AllCombinationSlotState : IState
    {        
        public Dictionary<int, CombinationSlotState> CombinationSlots { get; }
        
        public AllCombinationSlotState()
        {
            CombinationSlots = new Dictionary<int, CombinationSlotState>();
        }
        
        public AllCombinationSlotState(List serialized)
        {
            CombinationSlots = new Dictionary<int, CombinationSlotState>();
            foreach (var item in serialized.OfType<Dictionary>())
            {
                var slotState = new CombinationSlotState(item);
                CombinationSlots.Add(0, slotState);
            }
        }

        public bool TryGetRuneState(int runeId, out CombinationSlotState runeState)
        {
            runeState = CombinationSlots.TryGetValue(runeId, out var rs) ? rs : null;
            return runeState is not null;
        }

        public CombinationSlotState GetRuneState(int runeId)
        {
            return CombinationSlots.TryGetValue(runeId, out var runeState)
                ? runeState
                : throw new CombinationSlotNotFoundException($"Rune {runeId} not found in AllRuneState");
        }


        public void AddRuneState(Address address, int index = 0)
        {
            if (CombinationSlots.ContainsKey(index))
            {
                throw new DuplicatedCombinationSlotIndexException($"CombinationSlot Index {index} already exists");
            }

            CombinationSlots[index] = new CombinationSlotState(address, index);
        }

        public void AddRuneState(CombinationSlotState combinationSlotState)
        {
            if (CombinationSlots.ContainsKey(combinationSlotState.Index))
            {
                throw new DuplicatedCombinationSlotIndexException($"CombinationSlot Index {combinationSlotState.Index} already exists");
            }

            CombinationSlots[combinationSlotState.Index] = combinationSlotState;
        }

        public void SetRuneState(int runeId, int level)
        {
            if (!CombinationSlots.ContainsKey(runeId))
            {
                throw new CombinationSlotNotFoundException($"CombinationSlot Index {runeId} not exists.");
            }

            var rune = CombinationSlots[runeId];
            // rune.LevelUp(level - rune.Level);
        }

        public void SetRuneState(CombinationSlotState combinationSlotState)
        {
            if (!CombinationSlots.ContainsKey(combinationSlotState.Index))
            {
                throw new CombinationSlotNotFoundException($"CombinationSlot Index {combinationSlotState.Index} not exists.");
            }

            CombinationSlots[combinationSlotState.Index] = combinationSlotState;
        }
        
        public IValue Serialize()
        {
            return CombinationSlots.OrderBy(r => r.Key).
                Aggregate(List.Empty, (current, combinationSlot) => current.Add(combinationSlot.Value.Serialize()));            
        }
    }
}
