namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

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
            _agentAddress = privateKey.PublicKey.ToAddress();
            var agent = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            )
            {
                actionPoint = 0,
            };
            agent.avatarAddresses.Add(0, _avatarAddress);

            _initialState = new MockWorld();
            _initialState = LegacyModule.SetState(
                _initialState,
                Addresses.GameConfig,
                gameConfigState.Serialize());
            _initialState = AgentModule.SetAgentState(_initialState, _agentAddress, agent);
            _initialState = AvatarModule.SetAvatarState(_initialState, _avatarAddress, avatarState);

            foreach (var (key, value) in _sheets)
            {
                _initialState = LegacyModule.SetState(_initialState, Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public void Execute(bool useTradable, bool backward)
        {
            var avatarState = AvatarModule.GetAvatarState(_initialState, _avatarAddress);
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

            Assert.Equal(0, avatarState.actionPoint);

            IWorld state;
            if (backward)
            {
                state = AvatarModule.SetAvatarState(_initialState, _avatarAddress, avatarState);
            }
            else
            {
                state = LegacyModule.SetState(
                    _initialState,
                    _avatarAddress.Derive(LegacyInventoryKey),
                    avatarState.inventory.Serialize());
                state = LegacyModule.SetState(
                    state,
                    _avatarAddress.Derive(LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize());
                state = LegacyModule.SetState(
                    state,
                    _avatarAddress.Derive(LegacyQuestListKey),
                    avatarState.questList.Serialize());
                state = AvatarModule.SetAvatarStateV2(state, _avatarAddress, avatarState);
            }

            foreach (var (key, value) in _sheets)
            {
                state = LegacyModule.SetState(state, Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var action = new ChargeActionPoint()
            {
                avatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
            });

            var nextAvatarState = AvatarModule.GetAvatarStateV2(nextState, _avatarAddress);
            var gameConfigState = LegacyModule.GetGameConfigState(nextState);
            Assert.Equal(gameConfigState.ActionPointMax, nextAvatarState.actionPoint);
        }

        [Theory]
        [InlineData(false, false, false, false,  typeof(FailedLoadStateException))]
        [InlineData(true, false, false, false, typeof(NotEnoughMaterialException))]
        [InlineData(true, true, false, false, typeof(NotEnoughMaterialException))]
        [InlineData(true, false, true, true, typeof(ActionPointExceededException))]
        [InlineData(true, true, true, true, typeof(ActionPointExceededException))]
        public void Execute_Throw_Exception(bool useAvatarAddress, bool useTradable, bool enough, bool charge, Type exc)
        {
            var avatarState = AvatarModule.GetAvatarState(_initialState, _avatarAddress);

            Assert.Equal(0, avatarState.actionPoint);

            var avatarAddress = useAvatarAddress ? _avatarAddress : default;
            var state = _initialState;
            var row = _tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone);
            var apStone = useTradable
                ? ItemFactory.CreateTradableMaterial(row)
                : ItemFactory.CreateMaterial(row);
            if (apStone is TradableMaterial tradableMaterial)
            {
                if (!enough)
                {
                    tradableMaterial.RequiredBlockIndex = 10;
                }
            }

            if (enough)
            {
                avatarState.inventory.AddItem(apStone);
                state = AvatarModule.SetAvatarState(state, _avatarAddress, avatarState);
            }

            if (charge)
            {
                avatarState.actionPoint = LegacyModule.GetGameConfigState(state).ActionPointMax;
                state = AvatarModule.SetAvatarState(state, _avatarAddress, avatarState);
            }

            var action = new ChargeActionPoint()
            {
                avatarAddress = avatarAddress,
            };

            Assert.Throws(exc, () => action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                    Rehearsal = false,
                })
            );
        }

        [Fact]
        public void Rehearsal()
        {
            var action = new ChargeActionPoint
            {
                avatarAddress = _avatarAddress,
            };

            var updatedAddresses = new List<Address>()
            {
                _avatarAddress,
                _avatarAddress.Derive(LegacyInventoryKey),
                _avatarAddress.Derive(LegacyWorldInformationKey),
                _avatarAddress.Derive(LegacyQuestListKey),
            };

            var state = new MockWorld();

            var nextState = action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
                BlockIndex = 0,
                Rehearsal = true,
            });

            Assert.Equal(
                updatedAddresses.ToImmutableHashSet(),
                nextState.Delta.Accounts.Values.SelectMany(a => a.Delta.UpdatedAddresses));
        }
    }
}
