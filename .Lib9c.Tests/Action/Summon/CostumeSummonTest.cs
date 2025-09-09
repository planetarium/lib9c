namespace Lib9c.Tests.Action.Summon
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Exceptions;
    using Nekoyume.Action.Guild.Migration.LegacyModels;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Summon;
    using Xunit;
    using static SerializeKeys;

    public class CostumeSummonTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;
        private readonly Currency _currency;
        private TableSheets _tableSheets;
        private IWorld _initialState;

        public CostumeSummonTest()
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
                .MintAsset(context, _avatarAddress, 100 * Currencies.GetRune("RUNESTONE_FENRIR1"))
                .TransferAsset(
                    context,
                    Addresses.GoldCurrency,
                    _agentAddress,
                    gold.Currency * 1000
                )
                .SetDelegationMigrationHeight(0);

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
        [ClassData(typeof(ExecuteMemeber))]
        public void Execute(
            int groupId,
            int summonCount,
            int? materialId,
            int materialCount,
            int seed,
            Type expectedExc
        )
        {
            var random = new TestRandom(seed);
            var state = _initialState;
            state = state.SetLegacyState(
                Addresses.TableSheet.Derive(nameof(CostumeSummonSheet)),
                _tableSheets.CostumeSummonSheet.Serialize()
            );

            if (!(materialId is null) && materialCount > 0)
            {
                var materialSheet = _tableSheets.MaterialItemSheet;
                var material = materialSheet.OrderedList.FirstOrDefault(m => m.Id == materialId);
                _avatarState.inventory.AddItem(
                    ItemFactory.CreateItem(material, random),
                    materialCount * _tableSheets.CostumeSummonSheet[groupId].CostMaterialCount);
                state = state.SetAvatarState(_avatarAddress, _avatarState);
            }

            var action = new CostumeSummon
            {
                AvatarAddress = _avatarAddress,
                GroupId = groupId,
                SummonCount = summonCount,
            };

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
                var result = CostumeSummon.SimulateSummon(
                    string.Empty,
                    _tableSheets.CostumeItemSheet,
                    _tableSheets.CostumeSummonSheet[groupId],
                    summonCount,
                    new TestRandom(seed)
                );
                var inventory = nextState.GetAvatarState(_avatarAddress).inventory;
                foreach (var costume in result)
                {
                    inventory.TryGetNonFungibleItem(costume.ItemId, out Costume outItem);
                    Assert.Equal(costume, outItem);
                }

                inventory.TryGetItem((int)materialId!, out var resultMaterial);
                Assert.Equal(0, resultMaterial?.count ?? 0);

                var row = _tableSheets.CostumeSummonSheet[groupId];
                if (row.CostNcg > 0)
                {
                    Assert.True(nextState.GetBalance(Addresses.RewardPool, _currency) > 0 * _currency);
                }
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
                        RandomSeed = random.Seed,
                    });
                });
            }
        }

        [Theory]
        [InlineData(10, 11, 3, 1)]
        [InlineData(100, 110, 4, 2)]
        public void SimulateSummon_WithGuarantee(int summonCount, int resultCount, int minimumGrade, int guaranteeCount)
        {
            // Arrange
            var random = new TestRandom();
            var summonRow = CreateTestCostumeSummonRowWithGuarantee();

            // Act
            var result = CostumeSummon.SimulateSummon(
                string.Empty,
                _tableSheets.CostumeItemSheet,
                summonRow,
                summonCount,
                random
            );

            // Assert
            Assert.Equal(resultCount, result.Count()); // 10+1 rule applied
            // Check that at least the guaranteed number of costumes meets minimum grade requirement
            var guaranteedCostumes = result.Count(c => c.Grade >= minimumGrade);
            Assert.True(guaranteedCostumes >= guaranteeCount);
        }

        [Fact]
        public void SimulateSummon_NoEligibleCostumeRecipes_ShouldThrowException()
        {
            // Arrange
            var random = new TestRandom();
            var summonRow = CreateTestCostumeSummonRowWithLowGradesOnly();

            // Act & Assert
            var exception = Assert.Throws<System.InvalidOperationException>(() =>
                CostumeSummon.SimulateSummon(
                    string.Empty,
                    _tableSheets.CostumeItemSheet,
                    summonRow,
                    11, // Use 11 summons to trigger grade guarantee
                    random));

            Assert.Contains("No costume recipes found with grade >= 3", exception.Message);
            Assert.Contains("summon group 77777", exception.Message);
        }

        /// <summary>
        /// Creates a test CostumeSummonSheet.Row with grade guarantee settings for testing.
        /// </summary>
        /// <returns>A CostumeSummonSheet.Row with guarantee settings.</returns>
        private static CostumeSummonSheet.Row CreateTestCostumeSummonRowWithGuarantee()
        {
            var fields = new List<string>
            {
                "99999", // GroupId
                "600202", // CostMaterial
                "20", // CostMaterialCount
                "0", // CostNcg
                "GUARANTEE", // Grade guarantee marker
                "3", // MinimumGrade11
                "1", // GuaranteeCount11
                "4", // MinimumGrade110
                "2", // GuaranteeCount110
                "40100013", "1000", // Recipe1: Grade 1
                "40100014", "1000", // Recipe2: Grade 2
                "40100015", "1000", // Recipe3: Grade 3
                "40100016", "100",  // Recipe4: Grade 4
                "40100025", "10",   // Recipe5: Grade 5
            };

            var row = new CostumeSummonSheet.Row();
            row.Set(fields);
            return row;
        }

        /// <summary>
        /// Creates a test CostumeSummonSheet.Row with only low-grade costumes that don't meet guarantee requirements.
        /// </summary>
        private static CostumeSummonSheet.Row CreateTestCostumeSummonRowWithLowGradesOnly()
        {
            var fields = new List<string>
            {
                "77777", // GroupId
                "800201", // CostMaterial
                "10", // CostMaterialCount
                "0", // CostNcg
                "GUARANTEE", // Grade guarantee marker
                "3", // MinimumGrade11 (Epic grade)
                "1", // GuaranteeCount11
                "4", // MinimumGrade110 (Unique grade)
                "2", // GuaranteeCount110
                "171", "1000", // Recipe1: Low grade (1) - below minimum
                "172", "1000", // Recipe2: Normal grade (2) - below minimum
            };

            var row = new CostumeSummonSheet.Row();
            row.Set(fields);
            return row;
        }

        private class ExecuteMemeber : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new ()
            {
                new object[]
                {
                    50001, 1, 600202, 1, 1, null,
                },
                new object[]
                {
                    50001, 2, 600202, 2, 54, typeof(InvalidSummonCountException),
                },
                // Ten plus one
                new object[]
                {
                    50002,
                    10,
                    600203,
                    10,
                    0,
                    null,
                },
                // 100 + 10
                new object[]
                {
                    50002,
                    100,
                    600203,
                    100,
                    0,
                    null,
                },
                // fail by invalid group
                new object[]
                {
                    100003, 1, null, 0, 0,  typeof(RowNotInTableException),
                },
                // fail by not enough material
                new object[]
                {
                    50002, 1, 600202, 0, 0,  typeof(NotEnoughMaterialException),
                },
                // Fail by exceeding summon limit
                new object[]
                {
                    50002, 101, 600202, 22, 1,  typeof(InvalidSummonCountException),
                },
            };

            public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();
        }
    }
}
