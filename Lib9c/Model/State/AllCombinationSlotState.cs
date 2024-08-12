#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Module;
using Nekoyume.Module.CombinationSlot;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// This is new version of combination slot State. This state stores all combinationSlot states of an avatar.
    /// AllCombinationSlotState has all combinationSlotStates as dictionary and has methods to get/set/update each combinationSlotState.
    /// Use this with <see cref="Nekoyume.Module.CombinationSlotStateModule"/>.
    /// </summary>
    public class AllCombinationSlotState : IState, IEnumerable<CombinationSlotState>
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
                CombinationSlots.Add(slotState.Index, slotState);
            }
        }

        public CombinationSlotState GetCombinationSlotState(int slotStateIndex)
        {
            return CombinationSlots.TryGetValue(slotStateIndex, out var combinationSlotState)
                ? combinationSlotState
                : throw new CombinationSlotNotFoundException($"Rune {slotStateIndex} not found in AllCombinationSlotState");
        }

        public void AddCombinationSlotState(Address address, int index = 0)
        {
            if (CombinationSlots.ContainsKey(index))
            {
                throw new DuplicatedCombinationSlotIndexException($"CombinationSlot Index {index} already exists");
            }

            CombinationSlots[index] = new CombinationSlotState(address, index);
        }

        public void AddCombinationSlotState(CombinationSlotState combinationSlotState)
        {
            if (CombinationSlots.ContainsKey(combinationSlotState.Index))
            {
                throw new DuplicatedCombinationSlotIndexException($"CombinationSlot Index {combinationSlotState.Index} already exists");
            }

            CombinationSlots[combinationSlotState.Index] = combinationSlotState;
        }

        public void SetCombinationSlotState(CombinationSlotState combinationSlotState)
        {
            if (!CombinationSlots.ContainsKey(combinationSlotState.Index))
            {
                throw new CombinationSlotNotFoundException($"CombinationSlot Index {combinationSlotState.Index} not exists.");
            }

            CombinationSlots[combinationSlotState.Index] = combinationSlotState;
        }

        public IValue Serialize()
        {
            return CombinationSlots.OrderBy(kvp => kvp.Key).
                Aggregate(List.Empty, (current, combinationSlot) => current.Add(combinationSlot.Value.Serialize()));
        }

#region IEnumerable
        public IEnumerator<CombinationSlotState> GetEnumerator()
        {
            return CombinationSlots.Values.OrderBy(value => value.Index).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
#endregion IEnumerable

        /// <summary>
        /// 만약 AllCombinationSlotState가 없다면, 슬롯 확장 업데이트 전 4개의 슬롯을 가져와서 채워넣는다.
        /// </summary>
        /// <param name="avatarAddress">Migration을 진행할 아바타</param>
        /// <returns>Migration된 AllCombinationSlotState</returns>
        public static AllCombinationSlotState MigrationLegacyCombinationSlotState(Address avatarAddress)
        {
            var allCombinationSlotState = new AllCombinationSlotState();
            for (var i = 0; i < AvatarState.DefaultCombinationSlotCount; i++)
            {
                var combinationAddress = CombinationSlotState.DeriveAddress(avatarAddress, i);
                allCombinationSlotState.AddCombinationSlotState(new CombinationSlotState(combinationAddress, i));
            }

            return allCombinationSlotState;
        }
        
        public static AllCombinationSlotState MigrationLegacyCombinationSlotState(IWorldState worldState, Address avatarAddress)
        {
            var allCombinationSlotState = new AllCombinationSlotState();
            for (var i = 0; i < AvatarState.DefaultCombinationSlotCount; i++)
            {
                var combinationSlotState = worldState.GetCombinationSlotState(avatarAddress, i);
                allCombinationSlotState.AddCombinationSlotState(combinationSlotState);
            }

            return allCombinationSlotState;
        }
    }
}
