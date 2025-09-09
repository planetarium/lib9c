namespace Lib9c.Tests.Action.Summon
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Lib9c.Tests.Fixtures.TableCSV.Summon;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Exceptions;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData.Summon;
    using Xunit;

    public class AuraSummonTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;
        private readonly Currency _currency;
        private TableSheets _tableSheets;
        private IWorld _initialState;

        public AuraSummonTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.Address;
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            _avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            agentState.avatarAddresses.Add(0, _avatarAddress);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var gold = new GoldCurrencyState(_currency);

            var context = new ActionContext();
            _initialState = new World(MockUtil.MockModernWorldState)
                .SetAgentState(_agentAddress, agentState)
                .SetAvatarState(_avatarAddress, _avatarState)
                .SetLegacyState(GoldCurrencyState.Address, gold.Serialize())
                .MintAsset(context, GoldCurrencyState.Address, gold.Currency * 100000000000)
                .TransferAsset(
                    context,
                    Addresses.GoldCurrency,
                    _agentAddress,
                    gold.Currency * 1000
                );

            Assert.Equal(
                gold.Currency * 99999999000,
                _initialState.GetBalance(Addresses.GoldCurrency, gold.Currency)
            );
            Assert.Equal(
                gold.Currency * 1000,
                _initialState.GetBalance(_agentAddress, gold.Currency)
            );

            foreach (var (key, value) in sheets)
            {
                _initialState =
                    _initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData("V1", 10001)]
        [InlineData("V1", 10002)]
        [InlineData("V2", 10001)]
        [InlineData("V2", 10002)]
        public void CumulativeRatio(string version, int groupId)
        {
            var sheets = TableSheetsImporter.ImportSheets();
            if (version == "V1")
            {
                sheets[nameof(EquipmentSummonSheet)] = SummonSheetFixtures.V1;
            }
            else
            {
                sheets[nameof(EquipmentSummonSheet)] = SummonSheetFixtures.V2;
            }

            _tableSheets = new TableSheets(sheets);
            var sheet = _tableSheets.EquipmentSummonSheet;

            var targetRow = sheet.OrderedList.First(r => r.GroupId == groupId);

            for (var i = 1; i <= SummonSheet.Row.MaxRecipeCount; i++)
            {
                var sum = 0;
                for (var j = 0; j < i; j++)
                {
                    if (j < targetRow.Recipes.Count)
                    {
                        sum += targetRow.Recipes[j].Item2;
                    }
                }

                Assert.Equal(sum, targetRow.CumulativeRatio(i));
            }
        }

        [Theory]
        // success first group
        [InlineData("V1", 10001, 1, 800201, 1, 1, new[] { 10610000 }, null)]
        // success second group
        [InlineData("V1", 10002, 1, 600201, 1, 1, new[] { 10620001 }, null)]
        // Nine plus zero
        [InlineData(
            "V1",
            10001,
            9,
            800201,
            9,
            0,
            new[] { 10610000, 10610000, 10610000, 10610000, 10610000, 10610000, 10620000, 10620000, 10620000 },
            typeof(InvalidSummonCountException)
        )]
        [InlineData(
            "V1",
            10002,
            9,
            600201,
            9,
            0,
            new[] { 10620001, 10620001, 10620001, 10620001, 10620001, 10630001, 10630001, 10630001, 10630001 },
            typeof(InvalidSummonCountException)
        )]
        // Ten plus one
        [InlineData(
            "V1",
            10001,
            10,
            800201,
            10,
            0,
            new[] { 10610000, 10610000, 10610000, 10610000, 10610000, 10610000, 10610000, 10610000, 10620000, 10620000, 10620000, },
            null
        )]
        [InlineData(
            "V1",
            10002,
            10,
            600201,
            10,
            0,
            new[] { 10620001, 10620001, 10620001, 10620001, 10620001, 10620001, 10630001, 10620001, 10630001, 10630001, 10630001, },
            null
        )]
        // fail by invalid group
        [InlineData("V1", 100003, 1, null, 0, 0, new int[] { }, typeof(RowNotInTableException))]
        // fail by not enough material
        [InlineData("V1", 10001, 1, 800201, 0, 0, new int[] { }, typeof(NotEnoughMaterialException))]
        [InlineData("V1", 10001, 10, 800201, 0, 0, new int[] { }, typeof(NotEnoughMaterialException))]
        // Fail by exceeding summon limit
        [InlineData("V1", 10001, 101, 800201, 22, 1, new int[] { }, typeof(InvalidSummonCountException))]
        // 15 recipes
        [InlineData("V2", 10002, 1, 600201, 1, 5341, new[] { 10650006 }, null)]
        public void Execute(
            string version,
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
            var sheet = version switch
            {
                "V1" => SummonSheetFixtures.V1.Serialize(),
                "V2" => SummonSheetFixtures.V2.Serialize(),
                "V3" => SummonSheetFixtures.V3.Serialize(),
                _ => throw new ArgumentOutOfRangeException(nameof(version), version, null),
            };
            state = state.SetLegacyState(Addresses.TableSheet.Derive(nameof(EquipmentSummonSheet)), sheet);

            if (!(materialId is null) && materialCount > 0)
            {
                var materialSheet = _tableSheets.MaterialItemSheet;
                var material = materialSheet.OrderedList.FirstOrDefault(m => m.Id == materialId);
                _avatarState.inventory.AddItem(
                    ItemFactory.CreateItem(material, random),
                    materialCount * _tableSheets.EquipmentSummonSheet[groupId].CostMaterialCount);
                state = state.SetAvatarState(_avatarAddress, _avatarState);
            }

            var action = new AuraSummon(
                _avatarAddress,
                groupId,
                summonCount
            );

            if (expectedExc == null)
            {
                // Success
                var ctx = new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                };
                ctx.SetRandom(random);
                var nextState = action.Execute(ctx);

                var equipments = nextState.GetAvatarState(_avatarAddress).inventory.Equipments
                    .ToList();
                Assert.Equal(expectedEquipmentId.Length, equipments.Count);

                var checkedEquipments = new List<Guid>();
                foreach (var equipmentId in expectedEquipmentId)
                {
                    var resultEquipment = equipments.First(
                        e =>
                            e.Id == equipmentId && !checkedEquipments.Contains(e.ItemId)
                    );

                    checkedEquipments.Add(resultEquipment.ItemId);
                    Assert.NotNull(resultEquipment);
                    Assert.Equal(1, resultEquipment.RequiredBlockIndex);
                    Assert.True(resultEquipment.optionCountFromCombination > 0);
                }

                nextState.GetAvatarState(_avatarAddress).inventory
                    .TryGetItem((int)materialId!, out var resultMaterial);
                Assert.Equal(0, resultMaterial?.count ?? 0);
            }
            else
            {
                // Failure
                Assert.Throws(
                    expectedExc,
                    () =>
                    {
                        action.Execute(
                            new ActionContext
                            {
                                PreviousState = state,
                                Signer = _agentAddress,
                                BlockIndex = 1,
                                RandomSeed = random.Seed,
                            });
                    });
            }
        }

        [Theory]
        [InlineData(10, 11, 1, 3)]
        [InlineData(100, 110, 2, 4)]
        public void SimulateSummon_WithGradeGuarantee_ShouldGuaranteeMinimumGrade(int summonCount, int resultCount, int guaranteeCount, int minGrade)
        {
            // Arrange
            var random = new TestRandom();
            var summonRow = CreateTestSummonRowWithGuarantee();

            Assert.True(summonRow.UseGradeGuarantee(11));
            Assert.True(summonRow.UseGradeGuarantee(110));
            Assert.Equal((int)Grade.Epic, summonRow.MinimumGrade11);
            Assert.Equal((int)Grade.Unique, summonRow.MinimumGrade110);

            // Act
            var result = AuraSummon.SimulateSummon(
                "test",
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSheet,
                _tableSheets.EquipmentItemSubRecipeSheetV2,
                _tableSheets.EquipmentItemOptionSheet,
                _tableSheets.SkillSheet,
                summonRow,
                summonCount,
                random,
                0
            ).ToList();

            // Assert
            Assert.Equal(resultCount, result.Count); // 10+1 rule applied

            // Check that at least the guaranteed number of equipment meets minimum grade requirement
            Assert.True(result.Count(i => i.Item2.Grade >= minGrade) >= guaranteeCount);
        }

        [Theory]
        [InlineData(10, 11, 3)]
        [InlineData(100, 110, 4)]
        public void SimulateSummon_WithoutGradeGuarantee_ShouldUseOriginalLogic(int summonCount, int resultCount, int minGrade)
        {
            // Arrange
            var random = new TestRandom();
            var summonRow = CreateTestSummonRowWithoutGuarantee();
            Assert.False(summonRow.UseGradeGuarantee(11));
            Assert.False(summonRow.UseGradeGuarantee(110));

            // Act
            var result = AuraSummon.SimulateSummon(
                "test",
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSheet,
                _tableSheets.EquipmentItemSubRecipeSheetV2,
                _tableSheets.EquipmentItemOptionSheet,
                _tableSheets.SkillSheet,
                summonRow,
                summonCount,
                random,
                0
            ).ToList();

            // Assert
            Assert.Equal(resultCount, result.Count); // 10+1 rule applied
            // No grade guarantee, so we just verify the count is correct
            Assert.DoesNotContain(result, i => i.Item2.Grade >= minGrade);
        }

        /// <summary>
        /// Creates a test SummonSheet.Row with grade guarantee settings enabled.
        /// Uses actual recipe IDs from the test fixtures.
        /// </summary>
        private static SummonSheet.Row CreateTestSummonRowWithGuarantee()
        {
            var fields = new List<string>
            {
                "99999", // GroupId
                "800201", // CostMaterial
                "10", // CostMaterialCount
                "0", // CostNcg
                "GUARANTEE", // Grade guarantee marker
                "3", // MinimumGrade11
                "1", // GuaranteeCount11
                "4", // MinimumGrade110
                "2", // GuaranteeCount110
                "171", "1000", // Recipe1: Low grade (1) - from fixtures
                "172", "500",  // Recipe2: Low grade (1) - from fixtures
                "174", "200",  // Recipe3: Medium grade (2) - from fixtures
                "173", "1",  // Recipe4: High grade (3) - from fixtures
                "176", "1",  // Recipe5: Unique grade (4) - from fixtures
            };

            var row = new SummonSheet.Row();
            row.Set(fields);
            return row;
        }

        /// <summary>
        /// Creates a test SummonSheet.Row with grade guarantee settings enabled.
        /// Uses actual recipe IDs from the test fixtures.
        /// </summary>
        private static SummonSheet.Row CreateTestSummonRowWithoutGuarantee()
        {
            var fields = new List<string>
            {
                "99999", // GroupId
                "800201", // CostMaterial
                "10", // CostMaterialCount
                "0", // CostNcg
                "GUARANTEE", // Grade guarantee marker
                string.Empty, // MinimumGrade11
                string.Empty, // GuaranteeCount11
                string.Empty, // MinimumGrade110
                string.Empty, // GuaranteeCount110
                "171", "1000", // Recipe1: Low grade (1) - from fixtures
                "172", "500",  // Recipe2: Low grade (1) - from fixtures
                "174", "200",  // Recipe3: Medium grade (2) - from fixtures
                "173", "1",  // Recipe4: High grade (3) - from fixtures
                "176", "1",  // Recipe5: Unique grade (4) - from fixtures
            };

            var row = new SummonSheet.Row();
            row.Set(fields);
            return row;
        }
    }
}
