namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Crystal;
    using Xunit;

    public class GrindingTest
    {
        private readonly IRandom _random;
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AgentState _agentState;
        private readonly AvatarState _avatarState;
        private readonly Currency _crystalCurrency;
        private readonly Currency _ncgCurrency;
        private readonly IWorld _initialState;

        public GrindingTest()
        {
            _random = new TestRandom();
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            _agentAddress = new PrivateKey().Address;
            _avatarAddress = new PrivateKey().Address;
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _crystalCurrency = Currency.Legacy("CRYSTAL", 18, null);
#pragma warning restore CS0618
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);

            _agentState = new AgentState(_agentAddress);
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            _agentState.avatarAddresses.Add(0, _avatarAddress);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _ncgCurrency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(_ncgCurrency);

            _initialState = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(
                    Addresses.GetSheetAddress<CrystalMonsterCollectionMultiplierSheet>(),
                    _tableSheets.CrystalMonsterCollectionMultiplierSheet.Serialize())
                .SetLegacyState(
                    Addresses.GetSheetAddress<CrystalEquipmentGrindingSheet>(),
                    _tableSheets.CrystalEquipmentGrindingSheet.Serialize())
                .SetLegacyState(
                    Addresses.GetSheetAddress<MaterialItemSheet>(),
                    _tableSheets.MaterialItemSheet.Serialize())
                .SetLegacyState(
                    Addresses.GetSheetAddress<StakeRegularRewardSheet>(),
                    _tableSheets.StakeRegularRewardSheet.Serialize())
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
                .SetLegacyState(Addresses.GameConfig, gameConfigState.Serialize());
        }

        [Theory]
        [InlineData(0, false, 10)]
        [InlineData(0, true, 10)]
        [InlineData(2, false, 40)]
        public void Execute_Success(int itemLevel, bool equipped, int totalAsset)
        {
            var state = _initialState
                .SetAgentState(_agentAddress, _agentState)
                .SetActionPoint(_avatarAddress, 120);

            var itemRow = _tableSheets.EquipmentItemSheet.Values.First(r => r.Grade == 1);
            var equipment = (Equipment)ItemFactory.CreateItemUsable(itemRow, default, 1, itemLevel);
            equipment.equipped = equipped;
            _avatarState.inventory.AddItem(equipment);

            state = state.SetAvatarState(_avatarAddress, _avatarState);

            Assert.Equal(0 * _crystalCurrency, state.GetBalance(_avatarAddress, _crystalCurrency));

            Execute(state, _agentAddress, _avatarAddress, 1, false, _random, totalAsset, _tableSheets.MaterialItemSheet);
        }

        [Theory]
        // Multiply by StakeState.
        [InlineData(2, false, 0, 40)]
        [InlineData(0, false, 2, 15)]
        // Multiply by legacy MonsterCollectionState.
        [InlineData(2, true, 0, 40)]
        [InlineData(0, true, 2, 15)]
        public void Execute_Success_With_StakeState(
            int itemLevel,
            bool monsterCollect,
            int monsterCollectLevel,
            int totalAsset)
        {
            var context = new ActionContext();
            var state = _initialState
                .SetAgentState(_agentAddress, _agentState)
                .SetActionPoint(_avatarAddress, 120);

            var itemRow = _tableSheets.EquipmentItemSheet.Values.First(r => r.Grade == 1);
            var equipment = (Equipment)ItemFactory.CreateItemUsable(itemRow, default, 1, itemLevel);
            equipment.equipped = false;
            _avatarState.inventory.AddItem(equipment);

            state = state.SetAvatarState(_avatarAddress, _avatarState);

            Assert.Equal(0 * _crystalCurrency, state.GetBalance(_avatarAddress, _crystalCurrency));

            // StakeState;
            var stakeStateAddress = StakeState.DeriveAddress(_agentAddress);
            var stakeState = new StakeState(stakeStateAddress, 1);
            var requiredGold = _tableSheets.StakeRegularRewardSheet.OrderedRows
                .FirstOrDefault(r => r.Level == monsterCollectLevel)?.RequiredGold ?? 0;

            if (monsterCollect)
            {
                var mcAddress = MonsterCollectionState.DeriveAddress(_agentAddress, 0);
                state = state.SetLegacyState(
                    mcAddress,
                    new MonsterCollectionState(mcAddress, monsterCollectLevel, 1).Serialize()
                );

                if (requiredGold > 0)
                {
                    state = state.MintAsset(context, mcAddress, requiredGold * _ncgCurrency);
                }
            }
            else
            {
                state = state.SetLegacyState(stakeStateAddress, stakeState.SerializeV2());

                if (requiredGold > 0)
                {
                    state = state.MintAsset(
                        context,
                        stakeStateAddress,
                        requiredGold * _ncgCurrency
                    );
                }
            }

            Execute(state, _agentAddress, _avatarAddress, 1, false, _random, totalAsset, _tableSheets.MaterialItemSheet);
        }

        [Theory]
        // Required more ActionPoint.
        [InlineData(0, false, false, 0, typeof(NotEnoughActionPointException))]
        // Failed Charge AP.
        [InlineData(0, true, false, 0, typeof(NotEnoughMaterialException))]
        // Charge AP.
        [InlineData(0, true, true, 10, null)]
        public void Execute_With_ActionPoint(
            int ap,
            bool chargeAp,
            bool apStoneExist,
            int totalAsset,
            Type exc
        )
        {
            var state = _initialState
                .SetAgentState(_agentAddress, _agentState)
                .SetActionPoint(_avatarAddress, ap);

            var itemRow = _tableSheets.EquipmentItemSheet.Values.First(r => r.Grade == 1);
            var equipment = (Equipment)ItemFactory.CreateItemUsable(itemRow, default, 1);
            equipment.equipped = false;
            _avatarState.inventory.AddItem(equipment);

            if (chargeAp && apStoneExist)
            {
                var row = _tableSheets.MaterialItemSheet.Values.First(r =>
                    r.ItemSubType == ItemSubType.ApStone);
                var apStone = ItemFactory.CreateMaterial(row);
                _avatarState.inventory.AddItem(apStone);
            }

            state = state.SetAvatarState(_avatarAddress, _avatarState);

            Assert.Equal(0 * _crystalCurrency, state.GetBalance(_avatarAddress, _crystalCurrency));

            if (exc is null)
            {
                Execute(state, _agentAddress, _avatarAddress, 1, chargeAp, _random, totalAsset, _tableSheets.MaterialItemSheet);
            }
            else
            {
                Assert.Throws(exc, () =>
                    Execute(state, _agentAddress, _avatarAddress, 1, chargeAp, _random, totalAsset, _tableSheets.MaterialItemSheet));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(51)]
        public void Execute_Throw_InvalidItemCountException(int equipmentCount)
        {
            var context = new ActionContext();
            var state = _initialState
                .SetAgentState(_agentAddress, _agentState)
                .SetActionPoint(_avatarAddress, 120);

            var itemRow = _tableSheets.EquipmentItemSheet.Values.First(r => r.Grade == 1);
            var equipment = (Equipment)ItemFactory.CreateItemUsable(itemRow, default, 1, 2);
            equipment.equipped = false;
            _avatarState.inventory.AddItem(equipment);

            state = state.SetAvatarState(_avatarAddress, _avatarState);

            Assert.Equal(0 * _crystalCurrency, state.GetBalance(_avatarAddress, _crystalCurrency));

            // MonsterCollectionState;
            var monsterCollectLevel = 0;
            var requiredGold = _tableSheets.StakeRegularRewardSheet.OrderedRows
                .FirstOrDefault(r => r.Level == monsterCollectLevel)?.RequiredGold ?? 0;
            var mcAddress = MonsterCollectionState.DeriveAddress(_agentAddress, 0);
            state = state.SetLegacyState(
                mcAddress,
                new MonsterCollectionState(mcAddress, monsterCollectLevel, 1).Serialize()
            );

            if (requiredGold > 0)
            {
                state = state.MintAsset(context, mcAddress, requiredGold * _ncgCurrency);
            }

            Assert.Throws<InvalidItemCountException>(() =>
                Execute(state, _agentAddress, _avatarAddress, equipmentCount, false, _random, 200, _tableSheets.MaterialItemSheet));
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void Execute_Throw_FailedLoadStateException(bool agentExist, bool avatarExist)
        {
            var state = _initialState;
            if (agentExist)
            {
                state = state.SetAgentState(_agentAddress, _agentState);
            }

            if (avatarExist)
            {
                state = state.SetActionPoint(_avatarAddress, 120);

                var itemRow = _tableSheets.ConsumableItemSheet.Values.First(r => r.Grade == 1);
                var consumable = (Consumable)ItemFactory.CreateItemUsable(itemRow, default, 1);
                _avatarState.inventory.AddItem(consumable);

                state = state.SetAvatarState(_avatarAddress, _avatarState);

                Assert.Equal(0 * _crystalCurrency, state.GetBalance(_avatarAddress, _crystalCurrency));
            }

            Assert.Throws<FailedLoadStateException>(() =>
                Execute(state, _agentAddress, _avatarAddress, 1, false, _random, 0, _tableSheets.MaterialItemSheet));
        }

        [Theory]
        // Equipment not exist.
        [InlineData(false, 1, typeof(ItemDoesNotExistException))]
        // Locked equipment.
        [InlineData(true, 100, typeof(RequiredBlockIndexException))]
        public void Execute_Throw_Equipment_Exception(bool equipmentExist, long requiredBlockIndex, Type exc)
        {
            var state = _initialState
                .SetAgentState(_agentAddress, _agentState)
                .SetActionPoint(_avatarAddress, 120);

            if (equipmentExist)
            {
                var itemRow = _tableSheets.EquipmentItemSheet.Values.First(r => r.Grade == 1);
                var equipment = (Equipment)ItemFactory.CreateItemUsable(itemRow, default, requiredBlockIndex);
                equipment.equipped = false;
                _avatarState.inventory.AddItem(equipment);
            }
            else
            {
                var itemRow = _tableSheets.ConsumableItemSheet.Values.First(r => r.Grade == 1);
                var consumable = (Consumable)ItemFactory.CreateItemUsable(itemRow, default, requiredBlockIndex);
                _avatarState.inventory.AddItem(consumable);
            }

            state = state.SetAvatarState(_avatarAddress, _avatarState);

            Assert.Equal(0 * _crystalCurrency, state.GetBalance(_avatarAddress, _crystalCurrency));

            Assert.Throws(exc, () =>
                Execute(state, _agentAddress, _avatarAddress, 1, false, _random, 0, _tableSheets.MaterialItemSheet));
        }

        private static IWorld Execute(
            IWorld prevStates,
            Address agentAddress,
            Address avatarAddress,
            int equipmentCount,
            bool chargeAp,
            IRandom random,
            int totalAsset,
            MaterialItemSheet materialItemSheet)
        {
            var equipmentIds = new List<Guid>();
            for (int i = 0; i < equipmentCount; i++)
            {
                equipmentIds.Add(default);
            }

            Assert.Equal(equipmentCount, equipmentIds.Count);

            var action = new Grinding
            {
                AvatarAddress = avatarAddress,
                EquipmentIds = equipmentIds,
                ChargeAp = chargeAp,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousState = prevStates,
                Signer = agentAddress,
                BlockIndex = 1,
                RandomSeed = random.Seed,
            });

            var crystalCurrency = Currencies.Crystal; // CrystalCurrencyState.Address;
            var nextAvatarState = nextState.GetAvatarState(avatarAddress);
            FungibleAssetValue asset = totalAsset * crystalCurrency;

            Assert.Equal(asset, nextState.GetBalance(agentAddress, crystalCurrency));
            Assert.False(nextAvatarState.inventory.HasNonFungibleItem(default));
            Assert.Equal(115, nextState.GetActionPoint(avatarAddress));

            var mail = nextAvatarState.mailBox.OfType<GrindingMail>().First(i => i.id.Equals(action.Id));

            Assert.Equal(1, mail.ItemCount);
            Assert.Equal(asset, mail.Asset);

            var row = materialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone);
            Assert.False(nextAvatarState.inventory.HasItem(row.Id));

            // Todo : Check the material rewards
            return nextState;
        }
    }
}
