namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Action;
    using Lib9c.Model.Item;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Lib9c.TableData;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Xunit;

    public class ChargeActionPointTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly IWorld _initialState;

        public ChargeActionPointTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);

            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.Address;
            var agent = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            agent.avatarAddresses.Add(0, _avatarAddress);

            _initialState = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(Addresses.GameConfig, gameConfigState.Serialize())
                .SetAgentState(_agentAddress, agent)
                .SetAvatarState(_avatarAddress, avatarState);

            foreach (var (key, value) in _sheets)
            {
                _initialState = _initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute(bool useTradable)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var row = _tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone);
            if (useTradable)
            {
                var apStone = ItemFactory.CreateTradableMaterial(row);
                avatarState.inventory.AddItem(apStone);
            }
            else
            {
                var apStone = ItemFactory.CreateItem(row, new TestRandom());
                avatarState.inventory.AddItem(apStone);
            }

            Assert.False(_initialState.TryGetActionPoint(_avatarAddress, out var actionPoint));
            Assert.Equal(0L, actionPoint);

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState);
            foreach (var (key, value) in _sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var action = new ChargeActionPoint()
            {
                avatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    RandomSeed = 0,
                });

            Assert.True(nextState.TryGetActionPoint(_avatarAddress, out var nextActionPoint));
            Assert.Equal(DailyReward.ActionPointMax, nextActionPoint);
        }

        [Theory]
        [InlineData(false, false, false, false, typeof(FailedLoadStateException))]
        [InlineData(true, false, false, false, typeof(NotEnoughMaterialException))]
        [InlineData(true, true, false, false, typeof(NotEnoughMaterialException))]
        [InlineData(true, false, true, true, typeof(ActionPointExceededException))]
        [InlineData(true, true, true, true, typeof(ActionPointExceededException))]
        public void Execute_Throw_Exception(bool useAvatarAddress, bool useTradable, bool enoughApStone, bool actionPointIsAlreadyCharged, Type exc)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            _initialState.TryGetActionPoint(_avatarAddress, out var prevActionPoint);
            Assert.Equal(0L, prevActionPoint);

            var avatarAddress = useAvatarAddress ? _avatarAddress : default;
            var state = _initialState;
            var row = _tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone);
            var apStone = useTradable
                ? ItemFactory.CreateTradableMaterial(row)
                : ItemFactory.CreateMaterial(row);
            if (apStone is TradableMaterial tradableMaterial)
            {
                if (!enoughApStone)
                {
                    tradableMaterial.RequiredBlockIndex = 10;
                }
            }

            if (enoughApStone)
            {
                avatarState.inventory.AddItem(apStone);
                state = state.SetAvatarState(_avatarAddress, avatarState);
            }

            if (actionPointIsAlreadyCharged)
            {
                state = state.SetActionPoint(_avatarAddress, DailyReward.ActionPointMax);
            }

            var action = new ChargeActionPoint()
            {
                avatarAddress = avatarAddress,
            };

            Assert.Throws(
                exc,
                () => action.Execute(
                    new ActionContext()
                    {
                        PreviousState = state,
                        Signer = _agentAddress,
                        RandomSeed = 0,
                    })
            );
        }
    }
}
