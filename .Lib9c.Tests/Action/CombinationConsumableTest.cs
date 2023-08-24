namespace Lib9c.Tests.Action
{
    using System.Globalization;
    using System.Linq;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class CombinationConsumableTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly IRandom _random;
        private readonly TableSheets _tableSheets;
        private IWorld _initialState;

        public CombinationConsumableTest()
        {
            _agentAddress = new PrivateKey().ToAddress();
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

            var gameConfigState = new GameConfigState();

            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            );

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var gold = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618

            _initialState = new MockWorld();
            _initialState = AgentModule.SetAgentState(_initialState, _agentAddress, agentState);
            _initialState = AvatarModule.SetAvatarState(_initialState, _avatarAddress, avatarState);
            _initialState = LegacyModule.SetState(
                _initialState,
                slotAddress,
                new CombinationSlotState(
                    slotAddress,
                    GameConfig.RequireClearedStageLevel.CombinationConsumableAction).Serialize());
            _initialState = LegacyModule.SetState(
                _initialState,
                GameConfigState.Address,
                gold.Serialize());

            foreach (var (key, value) in sheets)
            {
                _initialState = LegacyModule.SetState(
                    _initialState,
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute(bool backward)
        {
            var avatarState = AvatarModule.GetAvatarState(_initialState, _avatarAddress);
            var row = _tableSheets.ConsumableItemRecipeSheet.Values.First();
            var costActionPoint = row.RequiredActionPoint;
            foreach (var materialInfo in row.Materials)
            {
                var materialRow = _tableSheets.MaterialItemSheet[materialInfo.Id];
                var material = ItemFactory.CreateItem(materialRow, _random);
                avatarState.inventory.AddItem(material, materialInfo.Count);
            }

            var previousActionPoint = avatarState.actionPoint;
            var previousResultConsumableCount =
                avatarState.inventory.Equipments.Count(e => e.Id == row.ResultConsumableItemId);
            var previousMailCount = avatarState.mailBox.Count;

            avatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                GameConfig.RequireClearedStageLevel.CombinationConsumableAction);

            IWorld previousState;
            if (backward)
            {
                previousState = AvatarModule.SetAvatarState(
                    _initialState,
                    _avatarAddress,
                    avatarState);
            }
            else
            {
                previousState = LegacyModule.SetState(
                    _initialState,
                    _avatarAddress.Derive(LegacyInventoryKey),
                    avatarState.inventory.Serialize());
                previousState = LegacyModule.SetState(
                    previousState,
                    _avatarAddress.Derive(LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize());
                previousState = LegacyModule.SetState(
                    previousState,
                    _avatarAddress.Derive(LegacyQuestListKey),
                    avatarState.questList.Serialize());
                previousState = AvatarModule.SetAvatarStateV2(
                    previousState,
                    _avatarAddress,
                    avatarState);
            }

            var action = new CombinationConsumable
            {
                avatarAddress = _avatarAddress,
                recipeId = row.Id,
                slotIndex = 0,
            };

            var nextState = action.Execute(
                new ActionContext
                {
                    PreviousState = new MockWorld(previousState),
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    Random = _random,
                });

            var slotState = LegacyModule.GetCombinationSlotState(nextState, _avatarAddress, 0);
            Assert.NotNull(slotState.Result);
            Assert.NotNull(slotState.Result.itemUsable);

            var consumable = (Consumable)slotState.Result.itemUsable;
            Assert.NotNull(consumable);

            var nextAvatarState = AvatarModule.GetAvatarStateV2(nextState, _avatarAddress);
            Assert.Equal(previousActionPoint - costActionPoint, nextAvatarState.actionPoint);
            Assert.Equal(previousMailCount + 1, nextAvatarState.mailBox.Count);
            Assert.IsType<CombinationMail>(nextAvatarState.mailBox.First());
            Assert.Equal(
                previousResultConsumableCount + 1,
                nextAvatarState.inventory.Consumables.Count(e => e.Id == row.ResultConsumableItemId));
        }
    }
}
