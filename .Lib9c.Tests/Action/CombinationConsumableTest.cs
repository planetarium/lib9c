namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Security.Cryptography;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class CombinationConsumableTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Address _slotAddress;
        private readonly Dictionary<string, string> _sheets;
        private readonly IRandom _random;
        private readonly TableSheets _tableSheets;
        private readonly IAccountStateDelta _initialState;
        private readonly ConsumableItemRecipeSheet.Row _consumableItemRecipeRow;

        public CombinationConsumableTest()
        {
            _agentAddress = default;
            _avatarAddress = _agentAddress.Derive("avatar");
            _slotAddress = _avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    0
                )
            );
            _sheets = TableSheetsImporter.ImportSheets();
            _random = new ItemEnhancementTest.TestRandom();
            _tableSheets = new TableSheets(_sheets);

            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses[0] = _avatarAddress;

            var gameConfigState = new GameConfigState();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            );
            _consumableItemRecipeRow = _tableSheets.ConsumableItemRecipeSheet.Values.First();
            foreach (var materialInfo in _consumableItemRecipeRow.Materials)
            {
                var materialRow = _tableSheets.MaterialItemSheet[materialInfo.Id];
                var material = ItemFactory.CreateItem(materialRow);
                avatarState.inventory.AddItem(material, materialInfo.Count);
            }

            const int requiredStage = GameConfig.RequireClearedStageLevel.CombinationConsumableAction;
            for (var i = 1; i < requiredStage + 1; i++)
            {
                avatarState.worldInformation.ClearStage(1, i, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
            }

            _initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(_slotAddress, new CombinationSlotState(_slotAddress, requiredStage).Serialize());

            foreach (var (key, value) in _sheets)
            {
                _initialState =
                    _initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Fact]
        public void Execute()
        {
            var action = new CombinationConsumable()
            {
                AvatarAddress = _avatarAddress,
                recipeId = _consumableItemRecipeRow.Id,
                slotIndex = 0,
            };

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                BlockIndex = 1,
                Random = _random,
            });

            var slotState = nextState.GetCombinationSlotState(_avatarAddress, 0);

            Assert.NotNull(slotState.Result);

            var consumable = (Consumable)slotState.Result.itemUsable;
            Assert.NotNull(consumable);
        }

        [Fact]
        public void Determinism()
        {
            var action = new CombinationConsumable()
            {
                AvatarAddress = _avatarAddress,
                recipeId = _consumableItemRecipeRow.Id,
                slotIndex = 0,
            };

            HashDigest<SHA256> stateRootHashA = ActionExecutionUtils.CalculateStateRootHash(action, previousStates: _initialState, signer: _agentAddress);
            HashDigest<SHA256> stateRootHashB = ActionExecutionUtils.CalculateStateRootHash(action, previousStates: _initialState, signer: _agentAddress);
            Assert.Equal(stateRootHashA, stateRootHashB);
        }
    }
}
