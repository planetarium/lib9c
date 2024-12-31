using Lib9c.Tests;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lib9c.DevExtensions.Action;
using Lib9c.Tests.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Helper;
using Nekoyume.Model.Faucet;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using static Lib9c.SerializeKeys;
using Libplanet.Mocks;

namespace Lib9c.DevExtensions.Tests.Action
{
    public class FaucetRuneTest
    {
        private readonly IWorld _initialState;
        private readonly Address _avatarAddress;
        private readonly RuneSheet _runeSheet;

        public FaucetRuneTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new World(MockUtil.MockModernWorldState);
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState =
                    _initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var tableSheets = new TableSheets(sheets);
            _runeSheet = _initialState.GetSheet<RuneSheet>();

            var agentAddress = new PrivateKey().Address;
            _avatarAddress = new PrivateKey().Address;
            var agentState = new AgentState(agentAddress);
            var avatarState = AvatarState.Create(
                _avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new PrivateKey().Address
            );
            agentState.avatarAddresses.Add(0, _avatarAddress);

            _initialState = _initialState
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(_avatarAddress, avatarState);
        }

        [Theory]
        [ClassData(typeof(FaucetRuneInfoGenerator))]
        public void Execute_FaucetRune(List<FaucetRuneInfo> faucetRuneInfos)
        {
            var action = new FaucetRune
            {
                AvatarAddress = _avatarAddress,
                FaucetRuneInfos = faucetRuneInfos,
            };
            var states = action.Execute(new ActionContext { PreviousState = _initialState, });
            foreach (var rune in faucetRuneInfos)
            {
                var expectedRune = RuneHelper.ToCurrency(
                    _runeSheet.OrderedList.First(r => r.Id == rune.RuneId));
                Assert.Equal(
                    rune.Amount * expectedRune,
                    states.GetBalance(_avatarAddress, expectedRune)
                );
            }
        }

        private class FaucetRuneInfoGenerator : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new List<object[]>
            {
                new object[]
                {
                    new List<FaucetRuneInfo>
                    {
                        new FaucetRuneInfo(10001, 10),
                    },
                },
                new object[]
                {
                    new List<FaucetRuneInfo>
                    {
                        new FaucetRuneInfo(10001, 10),
                        new FaucetRuneInfo(30001, 10),
                    },
                },
                new object[]
                {
                    new List<FaucetRuneInfo>
                    {
                        new FaucetRuneInfo(10001, 10),
                        new FaucetRuneInfo(10002, 10),
                        new FaucetRuneInfo(30001, 10),
                    },
                },
            };

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>data for each test case</returns>
            public IEnumerator<object[]> GetEnumerator()
            {
                return _data.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
