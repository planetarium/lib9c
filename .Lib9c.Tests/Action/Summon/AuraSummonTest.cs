namespace Lib9c.Tests.Action.Summon
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Exceptions;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class AuraSummonTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;
        private readonly Currency _currency;
        private IWorld _initialState;

        public AuraSummonTest()
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

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var gold = new GoldCurrencyState(_currency);

            var context = new ActionContext();
            _initialState = new MockWorld();
            _initialState = AgentModule.SetAgentState(_initialState, _agentAddress, agentState);
            _initialState = AvatarModule.SetAvatarState(
                _initialState,
                _avatarAddress,
                _avatarState,
                true,
                false,
                false,
                false);
            _initialState = LegacyModule.SetState(_initialState, GoldCurrencyState.Address, gold.Serialize());
            _initialState = LegacyModule.MintAsset(_initialState, context, GoldCurrencyState.Address, gold.Currency * 100000000000);
            _initialState = LegacyModule.TransferAsset(
                _initialState,
                context,
                Addresses.GoldCurrency,
                _agentAddress,
                gold.Currency * 1000);

            Assert.Equal(
                gold.Currency * 99999999000,
                LegacyModule.GetBalance(_initialState, Addresses.GoldCurrency, gold.Currency)
            );
            Assert.Equal(
                gold.Currency * 1000,
                LegacyModule.GetBalance(_initialState, _agentAddress, gold.Currency)
            );

            foreach (var (key, value) in sheets)
            {
                _initialState =
                    LegacyModule.SetState(_initialState, Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        // success first group
        [InlineData(10001, 1, 600201, 2, 1, new[] { 10620000 }, null)]
        [InlineData(10001, 2, 600201, 4, 54, new[] { 10630000, 10640000 }, null)]
        // success second group
        [InlineData(10002, 1, 600202, 2, 1, new[] { 10620001 }, null)]
        [InlineData(10002, 2, 600202, 4, 4, new[] { 10630001, 10640001 }, null)]
        // Nine plus zero
        [InlineData(
            10001,
            9,
            600201,
            18,
            0,
            new[] { 10620000, 10620000, 10620000, 10620000, 10620000, 10620000, 10630000, 10630000, 10630000 },
            null
        )]
        [InlineData(
            10002,
            9,
            600202,
            18,
            0,
            new[] { 10620001, 10620001, 10620001, 10620001, 10630001, 10630001, 10630001, 10640001, 10640001 },
            null
        )]
        // Ten plus one
        [InlineData(
            10001,
            10,
            600201,
            20,
            0,
            new[] { 10620000, 10620000, 10620000, 10620000, 10620000, 10620000, 10620000, 10630000, 10630000, 10630000, 10630000 },
            null
        )]
        [InlineData(
            10002,
            10,
            600202,
            20,
            0,
            new[] { 10620001, 10620001, 10620001, 10620001, 10630001, 10630001, 10630001, 10630001, 10640001, 10640001, 10640001 },
            null
        )]
        // fail by invalid group
        [InlineData(100003, 1, null, 0, 0, new int[] { }, typeof(RowNotInTableException))]
        // fail by not enough material
        [InlineData(10001, 1, 600201, 1, 0, new int[] { }, typeof(NotEnoughMaterialException))]
        [InlineData(10001, 2, 600201, 1, 0, new int[] { }, typeof(NotEnoughMaterialException))]
        // Fail by exceeding summon limit
        [InlineData(10001, 11, 600201, 22, 1, new int[] { }, typeof(InvalidSummonCountException))]
        public void Execute(
            int groupId,
            int summonCount,
            int? materialId,
            int materialCount,
            int seed,
            int[] expectedEquipmentId,
            Type expectedExc
        )
        {
            var random = new TestRandom(seed);
            var state = _initialState;

            if (!(materialId is null))
            {
                var materialSheet = _tableSheets.MaterialItemSheet;
                var material = materialSheet.OrderedList.FirstOrDefault(m => m.Id == materialId);
                _avatarState.inventory.AddItem(
                    ItemFactory.CreateItem(material, random),
                    materialCount
                );
                state = AvatarModule.SetAvatarState(
                    state,
                    _avatarAddress,
                    _avatarState,
                    true,
                    true,
                    true,
                    true);
            }

            var action = new AuraSummon(
                _avatarAddress,
                groupId,
                summonCount
            );

            if (expectedExc == null)
            {
                // Success
                var nextState = action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    Random = random,
                });

                var equipments = AvatarModule.GetAvatarState(nextState, _avatarAddress).inventory.Equipments
                    .ToList();
                Assert.Equal(expectedEquipmentId.Length, equipments.Count);

                var checkedEquipments = new List<Guid>();
                foreach (var equipmentId in expectedEquipmentId)
                {
                    var resultEquipment = equipments.First(e =>
                        e.Id == equipmentId && !checkedEquipments.Contains(e.ItemId)
                    );

                    checkedEquipments.Add(resultEquipment.ItemId);
                    Assert.NotNull(resultEquipment);
                    Assert.Equal(1, resultEquipment.RequiredBlockIndex);
                    Assert.True(resultEquipment.optionCountFromCombination > 0);
                }

                AvatarModule.GetAvatarState(nextState, _avatarAddress).inventory
                    .TryGetItem((int)materialId!, out var resultMaterial);
                Assert.Equal(0, resultMaterial?.count ?? 0);
            }
            else
            {
                // Failure
                Assert.Throws(expectedExc, () =>
                {
                    action.Execute(new ActionContext
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        BlockIndex = 1,
                        Random = random,
                    });
                });
            }
        }
    }
}
