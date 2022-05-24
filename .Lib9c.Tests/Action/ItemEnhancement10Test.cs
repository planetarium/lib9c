namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class ItemEnhancement10Test
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;
        private readonly Currency _currency;
        private IAccountStateDelta _initialState;

        public ItemEnhancement10Test()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.ToAddress();
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            agentState.avatarAddresses.Add(0, _avatarAddress);

            _currency = new Currency("NCG", 2, minter: null);
            var gold = new GoldCurrencyState(_currency);
            var slotAddress = _avatarAddress.Derive(string.Format(CultureInfo.InvariantCulture, CombinationSlotState.DeriveFormat, 0));

            _initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize())
                .SetState(slotAddress, new CombinationSlotState(slotAddress, 0).Serialize())
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .MintAsset(GoldCurrencyState.Address, gold.Currency * 100000000000)
                .TransferAsset(Addresses.GoldCurrency, _agentAddress, gold.Currency * 1000);

            Assert.Equal(gold.Currency * 99999999000, _initialState.GetBalance(Addresses.GoldCurrency, gold.Currency));
            Assert.Equal(gold.Currency * 1000, _initialState.GetBalance(_agentAddress, gold.Currency));

            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData(0, 1000, true, 0, 1, ItemEnhancement.EnhancementResult.Success, 0, 0)]
        [InlineData(6, 980, true, 0, 7, ItemEnhancement.EnhancementResult.Success, 0, 0)]
        [InlineData(0, 1000, false, 1, 1, ItemEnhancement.EnhancementResult.GreatSuccess, 0, 0)]
        [InlineData(6, 980, false, 10, 6, ItemEnhancement.EnhancementResult.Fail, 0, 3250)]
        [InlineData(6, 980, false, 10, 6, ItemEnhancement.EnhancementResult.Fail, 1, 3575)]
        public void Execute(int level, int expectedGold, bool backward, int randomSeed, int expectedLevel, ItemEnhancement.EnhancementResult expected, int monsterCollectLevel, int expectedCrystal)
        {
            var row = _tableSheets.EquipmentItemSheet.Values.First(r => r.Grade == 1);
            var equipment = (Equipment)ItemFactory.CreateItemUsable(row, default, 0, level);
            var materialId = Guid.NewGuid();
            var material = (Equipment)ItemFactory.CreateItemUsable(row, materialId, 0, level);

            _avatarState.inventory.AddItem(equipment, count: 1);
            _avatarState.inventory.AddItem(material, count: 1);

            var result = new CombinationConsumable5.ResultModel()
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
            };
            var preItemUsable = new Equipment((Dictionary)equipment.Serialize());

            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                _avatarState.Update(mail);
            }

            _avatarState.worldInformation.ClearStage(1, 1, 1, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);

            var slotAddress =
                _avatarAddress.Derive(string.Format(CultureInfo.InvariantCulture, CombinationSlotState.DeriveFormat, 0));

            Assert.Equal(level, equipment.level);

            if (backward)
            {
                _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());
            }
            else
            {
                _initialState = _initialState
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), _avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), _avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), _avatarState.questList.Serialize())
                    .SetState(_avatarAddress, _avatarState.SerializeV2());
            }

            if (monsterCollectLevel > 0)
            {
                var mcAddress = MonsterCollectionState.DeriveAddress(_agentAddress, 0);
                _initialState = _initialState.SetState(
                    mcAddress,
                    new MonsterCollectionState(mcAddress, monsterCollectLevel, 0).Serialize()
                );
            }

            var action = new ItemEnhancement10()
            {
                itemId = default,
                materialId = materialId,
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                BlockIndex = 1,
                Random = new TestRandom(randomSeed),
            });

            var slotState = nextState.GetCombinationSlotState(_avatarAddress, 0);
            var resultEquipment = (Equipment)slotState.Result.itemUsable;
            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.Equal(default, resultEquipment.ItemId);
            Assert.Equal(expectedLevel, resultEquipment.level);
            Assert.Equal(expectedGold * _currency, nextState.GetBalance(_agentAddress, _currency));
            Assert.Equal(
                (1000 - expectedGold) * _currency,
                nextState.GetBalance(Addresses.Blacksmith, _currency)
            );
            Assert.Equal(30, nextAvatarState.mailBox.Count);

            var costRow = _tableSheets.EnhancementCostSheetV2
                .OrderedList
                .First(x => x.Grade == 1 && x.Level == level + 1);
            var stateDict = (Dictionary)nextState.GetState(slotAddress);
            var slot = new CombinationSlotState(stateDict);
            var slotResult = (ItemEnhancement.ResultModel)slot.Result;
            Assert.Equal(expected, slotResult.enhancementResult);

            switch (slotResult.enhancementResult)
            {
                case ItemEnhancement.EnhancementResult.GreatSuccess:
                    var baseAtk = preItemUsable.StatsMap.BaseATK * (costRow.BaseStatGrowthMax.NormalizeFromTenThousandths() + 1);
                    var extraAtk = preItemUsable.StatsMap.AdditionalATK * (costRow.ExtraStatGrowthMax.NormalizeFromTenThousandths() + 1);
                    Assert.Equal((int)(baseAtk + extraAtk), resultEquipment.StatsMap.ATK);
                    break;
                case ItemEnhancement.EnhancementResult.Success:
                    var baseMinAtk = preItemUsable.StatsMap.BaseATK * (costRow.BaseStatGrowthMin.NormalizeFromTenThousandths() + 1);
                    var baseMaxAtk = preItemUsable.StatsMap.BaseATK * (costRow.BaseStatGrowthMax.NormalizeFromTenThousandths() + 1);
                    var extraMinAtk = preItemUsable.StatsMap.AdditionalATK * (costRow.ExtraStatGrowthMin.NormalizeFromTenThousandths() + 1);
                    var extraMaxAtk = preItemUsable.StatsMap.AdditionalATK * (costRow.ExtraStatGrowthMax.NormalizeFromTenThousandths() + 1);
                    Assert.InRange(resultEquipment.StatsMap.ATK, (int)(baseMinAtk + extraMinAtk), (int)(baseMaxAtk + extraMaxAtk) + 1);
                    break;
                case ItemEnhancement.EnhancementResult.Fail:
                    Assert.Equal(preItemUsable.StatsMap.ATK, resultEquipment.StatsMap.ATK);
                    break;
            }

            Assert.Equal(preItemUsable.TradableId, slotResult.preItemUsable.TradableId);
            Assert.Equal(preItemUsable.TradableId, resultEquipment.TradableId);
            Assert.Equal(costRow.Cost, slotResult.gold);
            Assert.Equal(expectedCrystal * CrystalCalculator.CRYSTAL, slotResult.CRYSTAL);
        }

        [Fact]
        public void Rehearsal()
        {
            var action = new ItemEnhancement10()
            {
                itemId = default,
                materialId = default,
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            var slotAddress = _avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    0
                )
            );
            var updatedAddresses = new List<Address>()
            {
                _agentAddress,
                _avatarAddress,
                slotAddress,
                _avatarAddress.Derive(LegacyInventoryKey),
                _avatarAddress.Derive(LegacyWorldInformationKey),
                _avatarAddress.Derive(LegacyQuestListKey),
                Addresses.Blacksmith,
            };

            var state = new State();

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                BlockIndex = 0,
                Rehearsal = true,
            });

            Assert.Equal(updatedAddresses.ToImmutableHashSet(), nextState.UpdatedAddresses);
        }
    }
}
