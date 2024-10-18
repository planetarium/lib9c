namespace Lib9c.Tests.Action
{
    using System.Globalization;
    using System.Linq;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class CombinationConsumableTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly IRandom _random;
        private readonly TableSheets _tableSheets;
        private IWorld _initialState;

        public CombinationConsumableTest()
        {
            _agentAddress = new PrivateKey().Address;
            _avatarAddress = _agentAddress.Derive("avatar");
            var slotAddress = _avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    0
                )
            );
            var sheets = TableSheetsImporter.ImportSheets();
            _random = new TestRandom();
            _tableSheets = new TableSheets(sheets);

            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses[0] = _avatarAddress;

            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.GetAvatarSheets(),
                default
            );

            var allSlotState = new AllCombinationSlotState();
            allSlotState.AddSlot(slotAddress);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var gold = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618

            _initialState = new World(MockUtil.MockModernWorldState)
                .SetAgentState(_agentAddress, agentState)
                .SetAvatarState(_avatarAddress, avatarState)
                .SetCombinationSlotState(_avatarAddress, allSlotState)
                .SetLegacyState(GameConfigState.Address, gold.Serialize());

            foreach (var (key, value) in sheets)
            {
                _initialState =
                    _initialState.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Fact]
        public void Execute()
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var row = _tableSheets.ConsumableItemRecipeSheet.Values.First();
            var costActionPoint = row.RequiredActionPoint;
            foreach (var materialInfo in row.Materials)
            {
                var materialRow = _tableSheets.MaterialItemSheet[materialInfo.Id];
                var material = ItemFactory.CreateItem(materialRow, _random);
                avatarState.inventory.AddItem(material, materialInfo.Count);
            }

            _initialState.TryGetActionPoint(_avatarAddress, out var previousActionPoint);
            var previousResultConsumableCount =
                avatarState.inventory.Equipments.Count(e => e.Id == row.ResultConsumableItemId);
            var previousMailCount = avatarState.mailBox.Count;

            avatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                GameConfig.RequireClearedStageLevel.CombinationConsumableAction);

            var previousState = _initialState.SetAvatarState(_avatarAddress, avatarState);

            var action = new CombinationConsumable
            {
                avatarAddress = _avatarAddress,
                recipeId = row.Id,
                slotIndex = 0,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousState = previousState,
                Signer = _agentAddress,
                BlockIndex = 1,
                RandomSeed = _random.Seed,
            });

            var allCombinationSlotState = nextState.GetAllCombinationSlotState(_avatarAddress);
            var slotState = allCombinationSlotState.GetSlot(0);
            Assert.NotNull(slotState.Result);
            Assert.NotNull(slotState.Result.itemUsable);

            var consumable = (Consumable)slotState.Result.itemUsable;
            Assert.NotNull(consumable);

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            if (nextState.TryGetActionPoint(_avatarAddress, out var nextActionPoint))
            {
                Assert.Equal(previousActionPoint - costActionPoint, nextActionPoint);
            }

            Assert.Equal(previousMailCount + 1, nextAvatarState.mailBox.Count);
            Assert.IsType<CombinationMail>(nextAvatarState.mailBox.First());
            Assert.Equal(
                previousResultConsumableCount + 1,
                nextAvatarState.inventory.Consumables.Count(e => e.Id == row.ResultConsumableItemId));
        }
    }
}
