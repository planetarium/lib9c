#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.Module;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.Model.State
{
    /// <summary>
    /// This is new version of combination slot State. This state stores all combinationSlot states of an avatar.
    /// AllCombinationSlotState has all combinationSlotStates as dictionary and has methods to get/set/update each combinationSlotState.
    /// Use this with <see cref="CombinationSlotStateModule"/>.
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

        public void UnlockSlot(Address avatarAddress, int index)
        {
            var targetSlot = TryGetSlot(index, out var combinationSlotState)
                ? combinationSlotState
                : null;

            if (targetSlot is null)
            {
                var slotAddr = Addresses.GetCombinationSlotAddress(avatarAddress, index);
                var newCombinationSlot = new CombinationSlotState(slotAddr, index);
                newCombinationSlot.Unlock();
                AddSlot(newCombinationSlot);
                return;
            }

            targetSlot.Unlock();
        }

        public bool TryGetSlot(int slotStateIndex, out CombinationSlotState? combinationSlotState)
        {
            combinationSlotState = CombinationSlots.TryGetValue(slotStateIndex, out var combinationSlot) ? combinationSlot : null;
            return combinationSlotState is not null;
        }

        public CombinationSlotState GetSlot(int slotStateIndex)
        {
            return CombinationSlots.TryGetValue(slotStateIndex, out var combinationSlotState)
                ? combinationSlotState
                : throw new CombinationSlotNotFoundException($"CombinationSlot {slotStateIndex} not found in AllCombinationSlotState");
        }

        public void AddSlot(Address address, int index = 0)
        {
            if (CombinationSlots.ContainsKey(index))
            {
                throw new DuplicatedCombinationSlotIndexException($"CombinationSlot Index {index} already exists");
            }

            CombinationSlots[index] = new CombinationSlotState(address, index);
        }

        public void AddSlot(CombinationSlotState combinationSlotState)
        {
            if (CombinationSlots.ContainsKey(combinationSlotState.Index))
            {
                throw new DuplicatedCombinationSlotIndexException($"CombinationSlot Index {combinationSlotState.Index} already exists");
            }

            CombinationSlots[combinationSlotState.Index] = combinationSlotState;
        }

        public void SetSlot(CombinationSlotState combinationSlotState)
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
        /// <param name="stateFactory">CombinationSlotState을 생성할 함수</param>
        /// <param name="avatarAddress">Migration을 진행할 아바타</param>
        /// <returns>Migration된 AllCombinationSlotState</returns>
        public static AllCombinationSlotState MigrationLegacySlotState(Func<int, CombinationSlotState?> stateFactory, Address avatarAddress)
        {
            var allCombinationSlotState = new AllCombinationSlotState();
            for (var i = 0; i < AvatarState.DefaultCombinationSlotCount; i++)
            {
                var combinationSlotState = stateFactory.Invoke(i);

                if (combinationSlotState == null)
                {
                    var combinationAddress = CombinationSlotState.DeriveAddress(avatarAddress, i);
                    combinationSlotState = new CombinationSlotState(combinationAddress, i);
                }

                combinationSlotState.Index = i;
                allCombinationSlotState.AddSlot(combinationSlotState);
            }

            return allCombinationSlotState;
        }

        /// <summary>
        /// 만약 AllCombinationSlotState가 없다면, 슬롯 확장 업데이트 전 4개의 슬롯을 가져와서 채워넣는다.
        /// </summary>
        /// <param name="worldState">Slot 상태를 가져올 world state</param>
        /// <param name="avatarAddress">Migration을 진행할 아바타</param>
        /// <returns>Migration된 AllCombinationSlotState</returns>
        public static AllCombinationSlotState MigrationLegacySlotState(IWorldState worldState, Address avatarAddress)
        {
            var allCombinationSlotState = new AllCombinationSlotState();
            for (var i = 0; i < AvatarState.DefaultCombinationSlotCount; i++)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var combinationSlotState = worldState.GetCombinationSlotStateLegacy(avatarAddress, i);
#pragma warning restore CS0618 // Type or member is obsolete

                if (combinationSlotState == null)
                {
                    var combinationAddress = CombinationSlotState.DeriveAddress(avatarAddress, i);
                    combinationSlotState = new CombinationSlotState(combinationAddress, i);
                }

                combinationSlotState.Index = i;
                allCombinationSlotState.AddSlot(combinationSlotState);
            }

            return allCombinationSlotState;
        }
    }
}
