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
        [InlineData(20001)]
        public void CumulativeRatio(int groupId)
        {
            var sheet = _tableSheets.SummonSheet;
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
                Addresses.TableSheet.Derive(nameof(SummonSheet)),
                _tableSheets.SummonSheet.Serialize()
            );

            if (!(materialId is null))
            {
                var materialSheet = _tableSheets.MaterialItemSheet;
                var material = materialSheet.OrderedList.FirstOrDefault(m => m.Id == materialId);
                _avatarState.inventory.AddItem(
                    ItemFactory.CreateItem(material, random),
                    materialCount * _tableSheets.SummonSheet[groupId].CostMaterialCount);
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
                    _tableSheets.SummonSheet[groupId],
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

        private class ExecuteMemeber : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new ()
            {
                new object[]
                {
                    20001, 1, 600201, 1, 1, null,
                },
                new object[]
                {
                    20001, 2, 600201, 2, 54, null,
                },
                // Nine plus zero
                new object[]
                {
                    20001,
                    9,
                    600201,
                    9,
                    0,
                    null,
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
                    20001, 11, 600201, 22, 1, typeof(InvalidSummonCountException),
                },
                new object[]
                {
                    10002, 1, 600201, 1, 1, typeof(SheetRowNotFoundException),
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
