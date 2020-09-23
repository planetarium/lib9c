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
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class RapidCombinationTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;
        private readonly IAccountStateDelta _initialState;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Equipment _equipment;

        public RapidCombinationTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);

            _agentAddress = default(Address);
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            agentState.avatarAddresses.Add(0, _avatarAddress);

            var material =
                ItemFactory.CreateMaterial(_tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Hourglass));
            avatarState.inventory.AddItem(material);

            avatarState.worldInformation.ClearStage(1, 1, 1, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);

            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var row = _tableSheets.EquipmentItemSheet.Values.First();
            _equipment = (Equipment)ItemFactory.CreateItemUsable(row, default, gameConfigState.HourglassPerBlock, 0);
            avatarState.inventory.AddItem(_equipment);

            var result = new CombinationConsumable.ResultModel
            {
                actionPoint = 0,
                gold = 0,
                materials = new Dictionary<Material, int>(),
                itemUsable = _equipment,
                recipeId = 0,
                itemType = ItemType.Equipment,
            };

            var requiredBlockIndex = gameConfigState.HourglassPerBlock;
            var mail = new CombinationMail(result, 0, default, requiredBlockIndex);
            result.id = mail.id;
            avatarState.Update(mail);

            var slotAddress =
                _avatarAddress.Derive(string.Format(CultureInfo.InvariantCulture, CombinationSlotState.DeriveFormat, 0));
            var slotState = new CombinationSlotState(slotAddress, 1);

            slotState.Update(result, 0, requiredBlockIndex);

            _initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(slotAddress, slotState.Serialize())
                .SetState(Addresses.GameConfig, gameConfigState.Serialize());

            foreach (var (key, value) in _sheets)
            {
                _initialState = _initialState.SetState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize()
                );
            }
        }

        [Fact]
        public void Execute()
        {
            var action = new RapidCombination()
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                BlockIndex = 1,
            });

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            var item = nextAvatarState.inventory.Equipments.First();

            Assert.Empty(nextAvatarState.inventory.Materials.Select(r => r.ItemSubType == ItemSubType.Hourglass));
            Assert.Equal(_equipment.ItemId, item.ItemId);
            Assert.Equal(1, item.RequiredBlockIndex);
        }

        [Fact]
        public void Determinism()
        {
            var action = new RapidCombination()
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            HashDigest<SHA256> stateRootHashA = ActionExecutionUtils.CalculateStateRootHash(action, previousStates: _initialState, signer: _agentAddress);
            HashDigest<SHA256> stateRootHashB = ActionExecutionUtils.CalculateStateRootHash(action, previousStates: _initialState, signer: _agentAddress);

            Assert.Equal(stateRootHashA, stateRootHashB);
        }
    }
}
