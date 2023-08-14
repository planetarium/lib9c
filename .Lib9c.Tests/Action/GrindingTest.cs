namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Crystal;
    using Xunit;
    using static Lib9c.SerializeKeys;

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
            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = new PrivateKey().ToAddress();
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
                gameConfigState,
                default
            );

            _agentState.avatarAddresses.Add(0, _avatarAddress);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _ncgCurrency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(_ncgCurrency);

            _initialState = new MockWorld();
            _initialState = LegacyModule.SetState(
                _initialState,
                Addresses.GetSheetAddress<CrystalMonsterCollectionMultiplierSheet>(),
                _tableSheets.CrystalMonsterCollectionMultiplierSheet.Serialize());
            _initialState = LegacyModule.SetState(
                _initialState,
                Addresses.GetSheetAddress<CrystalEquipmentGrindingSheet>(),
                _tableSheets.CrystalEquipmentGrindingSheet.Serialize());
            _initialState = LegacyModule.SetState(
                _initialState,
                Addresses.GetSheetAddress<MaterialItemSheet>(),
                _tableSheets.MaterialItemSheet.Serialize());
            _initialState = LegacyModule.SetState(
                _initialState,
                Addresses.GetSheetAddress<StakeRegularRewardSheet>(),
                _tableSheets.StakeRegularRewardSheet.Serialize());
            _initialState = LegacyModule.SetState(
                _initialState,
                Addresses.GoldCurrency,
                goldCurrencyState.Serialize());
            _initialState = LegacyModule.SetState(
                _initialState,
                Addresses.GameConfig,
                gameConfigState.Serialize());
        }

        [Theory]
        [InlineData(true, true, 120, false, false, true, 1, 0, false, false, false, 0, 10, 1, null)]
        [InlineData(true, true, 120, false, false, true, 1, 2, false, false, false, 0, 40, 1, null)]
        // Multiply by StakeState.
        [InlineData(true, true, 120, false, false, true, 1, 2, false, true, false, 0, 40, 1, null)]
        [InlineData(true, true, 120, false, false, true, 1, 0, false, true, false, 2, 15, 1, null)]
        // Multiply by legacy MonsterCollectionState.
        [InlineData(true, true, 120, false, false, true, 1, 2, false, false, true, 0, 40, 1, null)]
        [InlineData(true, true, 120, false, false, true, 1, 0, false, false, true, 2, 15, 1, null)]
        // Charge AP.
        [InlineData(true, true, 0, true, true, true, 1, 0, false, false, false, 0, 10, 1, null)]
        // Invalid equipment count.
        [InlineData(true, true, 120, false, false, true, 1, 2, false, false, true, 0, 200, 0, typeof(InvalidItemCountException))]
        [InlineData(true, true, 120, false, false, true, 1, 2, false, false, true, 0, 200, 11, typeof(InvalidItemCountException))]
        // AgentState not exist.
        [InlineData(false, true, 120, false, false, false, 1, 0, false, false, false, 0, 0, 1, typeof(FailedLoadStateException))]
        // AvatarState not exist.
        [InlineData(true, false, 120, false, false, false, 1, 0, false, false, false, 0, 0, 1, typeof(FailedLoadStateException))]
        // Required more ActionPoint.
        [InlineData(true, true, 0, false, false, false, 1, 0, false, false, false, 0, 0, 1, typeof(NotEnoughActionPointException))]
        // Failed Charge AP.
        [InlineData(true, true, 0, true, false, false, 1, 0, false, false, false, 0, 100, 1, typeof(NotEnoughMaterialException))]
        // Equipment not exist.
        [InlineData(true, true, 120, false, false, false, 1, 0, false, false, false, 0, 0, 1, typeof(ItemDoesNotExistException))]
        // Locked equipment.
        [InlineData(true, true, 120, false, false, true, 100, 0, false, false, false, 0, 0, 1, typeof(RequiredBlockIndexException))]
        public void Execute(
            bool agentExist,
            bool avatarExist,
            int ap,
            bool chargeAp,
            bool apStoneExist,
            bool equipmentExist,
            long requiredBlockIndex,
            int itemLevel,
            bool equipped,
            bool stake,
            bool monsterCollect,
            int monsterCollectLevel,
            int totalAsset,
            int equipmentCount,
            Type exc
        )
        {
            var context = new ActionContext();
            var state = _initialState;
            if (agentExist)
            {
                state = AgentModule.SetAgentState(state, _agentAddress, _agentState);
            }

            if (avatarExist)
            {
                _avatarState.actionPoint = ap;

                if (equipmentExist)
                {
                    var row = _tableSheets.EquipmentItemSheet.Values.First(r => r.Grade == 1);
                    var equipment = (Equipment)ItemFactory.CreateItemUsable(row, default, requiredBlockIndex, itemLevel);
                    equipment.equipped = equipped;
                    _avatarState.inventory.AddItem(equipment, count: 1);
                }
                else
                {
                    var row = _tableSheets.ConsumableItemSheet.Values.First(r => r.Grade == 1);
                    var consumable = (Consumable)ItemFactory.CreateItemUsable(row, default, requiredBlockIndex, itemLevel);
                    _avatarState.inventory.AddItem(consumable, count: 1);
                }

                if (chargeAp && apStoneExist)
                {
                    var row = _tableSheets.MaterialItemSheet.Values.First(r =>
                        r.ItemSubType == ItemSubType.ApStone);
                    var apStone = ItemFactory.CreateMaterial(row);
                    _avatarState.inventory.AddItem(apStone);
                }

                state = LegacyModule.SetState(
                    state,
                    _avatarAddress.Derive(LegacyInventoryKey),
                    _avatarState.inventory.Serialize());
                state = LegacyModule.SetState(
                    state,
                    _avatarAddress.Derive(LegacyWorldInformationKey),
                    _avatarState.worldInformation.Serialize());
                state = LegacyModule.SetState(
                    state,
                    _avatarAddress.Derive(LegacyQuestListKey),
                    _avatarState.questList.Serialize());
                state = AvatarModule.SetAvatarStateV2(state, _avatarAddress, _avatarState);

                Assert.Equal(
                    0 * _crystalCurrency,
                    LegacyModule.GetBalance(state, _avatarAddress, _crystalCurrency));
            }

            if (stake || monsterCollect)
            {
                // StakeState;
                var stakeStateAddress = StakeState.DeriveAddress(_agentAddress);
                var stakeState = new StakeState(stakeStateAddress, 1);
                var requiredGold = _tableSheets.StakeRegularRewardSheet.OrderedRows
                    .FirstOrDefault(r => r.Level == monsterCollectLevel)?.RequiredGold ?? 0;

                if (stake)
                {
                    state = LegacyModule.SetState(state, stakeStateAddress, stakeState.SerializeV2());

                    if (requiredGold > 0)
                    {
                        state = LegacyModule.MintAsset(
                            state,
                            context,
                            stakeStateAddress,
                            requiredGold * _ncgCurrency
                        );
                    }
                }

                if (monsterCollect)
                {
                    var mcAddress = MonsterCollectionState.DeriveAddress(_agentAddress, 0);
                    state = LegacyModule.SetState(
                        state,
                        mcAddress,
                        new MonsterCollectionState(mcAddress, monsterCollectLevel, 1).Serialize()
                    );

                    if (requiredGold > 0)
                    {
                        state = LegacyModule.MintAsset(state, context, mcAddress, requiredGold * _ncgCurrency);
                    }
                }
            }

            var equipmentIds = new List<Guid>();
            for (int i = 0; i < equipmentCount; i++)
            {
                equipmentIds.Add(default);
            }

            Assert.Equal(equipmentCount, equipmentIds.Count);

            var action = new Grinding
            {
                AvatarAddress = _avatarAddress,
                EquipmentIds = equipmentIds,
                ChargeAp = chargeAp,
            };

            var world = new MockWorld(state);
            if (exc is null)
            {
                var nextWorld = action.Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    Random = _random,
                });
                var nextAccount = nextWorld.GetAccount(ReservedAddresses.LegacyAccount);

                var nextAvatarState = AvatarModule.GetAvatarStateV2(nextWorld, _avatarAddress);
                FungibleAssetValue asset = totalAsset * _crystalCurrency;

                Assert.Equal(asset, nextAccount.GetBalance(_agentAddress, _crystalCurrency));
                Assert.False(nextAvatarState.inventory.HasNonFungibleItem(default));
                Assert.Equal(115, nextAvatarState.actionPoint);

                var mail = nextAvatarState.mailBox.OfType<GrindingMail>().First(i => i.id.Equals(action.Id));

                Assert.Equal(1, mail.ItemCount);
                Assert.Equal(asset, mail.Asset);

                var row = _tableSheets.MaterialItemSheet.Values.First(r =>
                    r.ItemSubType == ItemSubType.ApStone);
                Assert.False(nextAvatarState.inventory.HasItem(row.Id));
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext
                {
                    PreviousState = world,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    Random = _random,
                }));
            }
        }
    }
}
