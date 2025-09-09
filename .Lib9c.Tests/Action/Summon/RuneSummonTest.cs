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
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Summon;
    using Xunit;
    using static SerializeKeys;

    public class RuneSummonTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;
        private readonly Currency _currency;
        private TableSheets _tableSheets;
        private IWorld _initialState;

        public RuneSummonTest()
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
                Addresses.TableSheet.Derive(nameof(RuneSummonSheet)),
                _tableSheets.RuneSummonSheet.Serialize()
            );

            if (!(materialId is null) && materialCount > 0)
            {
                var materialSheet = _tableSheets.MaterialItemSheet;
                var material = materialSheet.OrderedList.FirstOrDefault(m => m.Id == materialId);
                _avatarState.inventory.AddItem(
                    ItemFactory.CreateItem(material, random),
                    materialCount * _tableSheets.RuneSummonSheet[groupId].CostMaterialCount);
                state = state.SetAvatarState(_avatarAddress, _avatarState);
            }

            var action = new RuneSummon
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
                var result = RuneSummon.SimulateSummon(
                    _tableSheets.RuneSheet,
                    _tableSheets.RuneSummonSheet[groupId],
                    summonCount,
                    new TestRandom(seed)
                );
                foreach (var pair in result)
                {
                    var currency = pair.Key;
                    var prevBalance = state.GetBalance(_avatarAddress, currency);
                    var balance = nextState.GetBalance(_avatarAddress, currency);
                    Assert.Equal(currency * pair.Value, balance - prevBalance);
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
        [InlineData(10, 11, 3, 1)]
        [InlineData(100, 110, 4, 2)]
        public void SimulateSummon_WithGuarantee(int summonCount, int resultCount, int minimumGrade, int guaranteeCount)
        {
            // Arrange
            var random = new TestRandom();
            var summonRow = CreateTestRuneSummonRowWithGuarantee();

            // Act
            var result = RuneSummon.SimulateSummon(
                _tableSheets.RuneSheet,
                summonRow,
                summonCount,
                random,
                runeListSheet: _tableSheets.RuneListSheet
            );

            // Assert
            Assert.Equal(resultCount * 10, result.Values.Sum()); // 10+1 rule applied, each rune gives 10 items
            // Check that at least the guaranteed number of runes meets minimum grade requirement
            var guaranteedRunes = 0;
            foreach (var (currency, quantity) in result)
            {
                var runeId = ExtractRuneIdFromCurrency(currency);
                if (runeId.HasValue && _tableSheets.RuneListSheet.TryGetValue(runeId.Value, out var runeListRow))
                {
                    if (runeListRow.Grade >= minimumGrade)
                    {
                        guaranteedRunes += quantity; // Each summon gives 10 runes
                    }
                }
            }

            Assert.True(guaranteedRunes >= guaranteeCount * 10);
        }

        /// <summary>
        /// Creates a test RuneSummonSheet.Row with grade guarantee settings enabled.
        /// </summary>
        private static RuneSummonSheet.Row CreateTestRuneSummonRowWithGuarantee()
        {
            var fields = new List<string>
            {
                "99999", // GroupId
                "600201", // CostMaterial
                "20", // CostMaterialCount
                "0", // CostNcg
                "GUARANTEE", // Grade guarantee marker
                "3", // MinimumGrade11
                "1", // GuaranteeCount11
                "4", // MinimumGrade110
                "2", // GuaranteeCount110
                "10021", "1000", // Recipe1: Low grade (1)
                "10022", "1000", // Recipe2: Normal grade (2)
                "10023", "1",    // Recipe3: High grade (3)
                "10024", "1",    // Recipe4: Unique grade (4)
            };

            var row = new RuneSummonSheet.Row();
            row.Set(fields);
            return row;
        }

        /// <summary>
        /// Extracts rune ID from currency ticker.
        /// </summary>
        private int? ExtractRuneIdFromCurrency(Currency currency)
        {
            // Extract rune ID from ticker by finding the corresponding rune in RuneSheet
            // The ticker format is like "RUNESTONE_FENRIR1" or similar
            var ticker = currency.Ticker;

            // Find the rune ID by matching the ticker in the RuneSheet
            var runeRow = _tableSheets.RuneSheet.OrderedList.FirstOrDefault(r => r.Ticker == ticker);
            return runeRow?.Id;
        }

        private class ExecuteMemeber : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new ()
            {
                new object[]
                {
                    20001, 1, 600201, 1, 1, null,
                },
                // Nine plus zero
                new object[]
                {
                    20001,
                    9,
                    600201,
                    9,
                    0,
                    typeof(InvalidSummonCountException),
                },
                // Ten plus one
                new object[]
                {
                    20001,
                    10,
                    600201,
                    10,
                    0,
                    null,
                },
                // fail by invalid group
                new object[]
                {
                    100003, 1, null, 0, 0, typeof(RowNotInTableException),
                },
                // fail by not enough material
                new object[]
                {
                    20001, 1, 600201, 0, 0, typeof(NotEnoughMaterialException),
                },
                // Fail by exceeding summon limit
                new object[]
                {
                    20001, 101, 600201, 22, 1,  typeof(InvalidSummonCountException),
                },
            };

            public IEnumerator<object[]> GetEnumerator()
            {
                return _data.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _data.GetEnumerator();
            }
        }
    }
}
