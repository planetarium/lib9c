namespace Lib9c.Tests.Action
{
    using System;
    using System.Globalization;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Exceptions;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class GrantEquipmentPotentialTest
    {
        private const int CostMaterialId = 301000;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;
        private readonly IWorld _initialState;

        public GrantEquipmentPotentialTest()
        {
            _agentAddress = new PrivateKey().Address;
            _avatarAddress = _agentAddress.Derive(
                string.Format(CultureInfo.InvariantCulture, CreateAvatar.DeriveFormat, 0));
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);

            var agentState = new AgentState(_agentAddress)
            {
                avatarAddresses = { [0] = _avatarAddress },
            };
            var avatarState = AvatarState.Create(
                _avatarAddress, _agentAddress, 0, _tableSheets.GetAvatarSheets(), default);

            _initialState = new World(MockUtil.MockModernWorldState)
                .SetAgentState(_agentAddress, agentState)
                .SetAvatarState(_avatarAddress, avatarState);

            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState.SetLegacyState(
                    Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Fact]
        public void Execute_GrantsPotential()
        {
            var row = FirstEquipment(ItemSubType.Weapon, gradeInSheet: true);
            var gradeRow = _tableSheets.EquipmentPotentialGradeSheet[row.Grade];
            var (state, itemId) = PrepareEquipment(row, gradeRow.CostMaterialCount + 5);

            var next = Execute(state, itemId);

            var avatarState = next.GetAvatarState(_avatarAddress);
            Assert.True(avatarState.inventory.TryGetNonFungibleItem<Equipment>(itemId, out var eq));
            Assert.Equal(gradeRow.SlotCount, eq.Potential.UnlockedSlotCount);
            Assert.Equal(gradeRow.SlotCount, eq.Potential.Slots.Count);
            foreach (var slot in eq.Potential.Slots)
            {
                var poolRow = _tableSheets.EquipmentPotentialOptionPoolSheet[slot.OptionRowId];
                Assert.Equal(ItemSubType.Weapon, poolRow.ItemSubType);
                Assert.InRange(slot.Value, poolRow.ValueMin, poolRow.ValueMax);
            }

            // Minted CostMaterialCount + 5, so exactly 5 should remain after the grant.
            var remaining = avatarState.inventory.Items
                .Where(i => i.item is Material m && m.Id == CostMaterialId)
                .Sum(i => i.count);
            Assert.Equal(5, remaining);
        }

        [Fact]
        public void Execute_ReGrant_OverwritesAndRechargesCost()
        {
            var row = FirstEquipment(ItemSubType.Weapon, gradeInSheet: true);
            var gradeRow = _tableSheets.EquipmentPotentialGradeSheet[row.Grade];
            // Enough material for two grants, plus a remainder to assert against.
            var (state, itemId) = PrepareEquipment(row, (gradeRow.CostMaterialCount * 2) + 3);

            var firstState = Execute(state, itemId, seed: 1);
            var secondState = Execute(firstState, itemId, seed: 2);

            var avatarState = secondState.GetAvatarState(_avatarAddress);
            Assert.True(avatarState.inventory.TryGetNonFungibleItem<Equipment>(itemId, out var eq));

            // Overwrite (not accumulation): the slot count stays the grade's slot count.
            Assert.Equal(gradeRow.SlotCount, eq.Potential.UnlockedSlotCount);
            Assert.Equal(gradeRow.SlotCount, eq.Potential.Slots.Count);

            // Cost is charged once per grant, i.e. twice in total.
            var remaining = avatarState.inventory.Items
                .Where(i => i.item is Material m && m.Id == CostMaterialId)
                .Sum(i => i.count);
            Assert.Equal(3, remaining);
        }

        [Fact]
        public void Execute_NotEnoughMaterial_Throws()
        {
            var row = FirstEquipment(ItemSubType.Weapon, gradeInSheet: true);
            var gradeRow = _tableSheets.EquipmentPotentialGradeSheet[row.Grade];
            var (state, itemId) = PrepareEquipment(row, gradeRow.CostMaterialCount - 1);

            Assert.Throws<NotEnoughItemException>(() => Execute(state, itemId));
        }

        [Fact]
        public void Execute_GradeWithoutSlots_Throws()
        {
            var row = FirstEquipment(ItemSubType.Weapon, gradeInSheet: false);
            var (state, itemId) = PrepareEquipment(row, 1000);

            Assert.Throws<InvalidItemTypeException>(() => Execute(state, itemId));
        }

        [Fact]
        public void Execute_IneligibleSubType_Throws()
        {
            // Aura is an Equipment subclass but is intentionally excluded from potential options.
            var row = FirstEquipment(ItemSubType.Aura, gradeInSheet: true);
            var (state, itemId) = PrepareEquipment(row, 1000);

            Assert.Throws<InvalidItemTypeException>(() => Execute(state, itemId));
        }

        [Fact]
        public void Execute_ItemNotOwned_Throws()
        {
            Assert.Throws<ItemDoesNotExistException>(
                () => Execute(_initialState, Guid.NewGuid()));
        }

        [Fact]
        public void Execute_Deterministic_SameSeedSameResult()
        {
            var row = FirstEquipment(ItemSubType.Weapon, gradeInSheet: true);
            var gradeRow = _tableSheets.EquipmentPotentialGradeSheet[row.Grade];
            var (state, itemId) = PrepareEquipment(row, gradeRow.CostMaterialCount);

            var a = Execute(state, itemId, seed: 42);
            var b = Execute(state, itemId, seed: 42);

            a.GetAvatarState(_avatarAddress).inventory
                .TryGetNonFungibleItem<Equipment>(itemId, out var eqA);
            b.GetAvatarState(_avatarAddress).inventory
                .TryGetNonFungibleItem<Equipment>(itemId, out var eqB);
            Assert.Equal(eqA.Potential, eqB.Potential);
        }

        private EquipmentItemSheet.Row FirstEquipment(ItemSubType subType, bool gradeInSheet)
        {
            return _tableSheets.EquipmentItemSheet.OrderedList.First(
                r => r.ItemSubType == subType &&
                    (gradeInSheet
                        ? r.Grade >= 3 && r.Grade <= 8
                        : r.Grade < 3 || r.Grade > 8));
        }

        private (IWorld state, Guid itemId) PrepareEquipment(
            EquipmentItemSheet.Row row, int materialCount)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var equipment = (Equipment)ItemFactory.CreateItemUsable(row, Guid.NewGuid(), 0);
            avatarState.inventory.AddItem(equipment);
            if (materialCount > 0)
            {
                var material = ItemFactory.CreateMaterial(
                    _tableSheets.MaterialItemSheet, CostMaterialId);
                avatarState.inventory.AddItem(material, materialCount);
            }

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState);
            return (state, equipment.ItemId);
        }

        private IWorld Execute(IWorld state, Guid itemId, int seed = 0, long blockIndex = 1)
        {
            var action = new GrantEquipmentPotential
            {
                AvatarAddress = _avatarAddress,
                ItemId = itemId,
            };
            return action.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = _agentAddress,
                BlockIndex = blockIndex,
                RandomSeed = seed,
            });
        }
    }
}
