namespace Lib9c.Tests.Action
{
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static SerializeKeys;

    public class ChangeAvatarNameTest
    {
        private readonly IAccountStateDelta _initialStates;
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;

        public ChangeAvatarNameTest()
        {
            _initialStates = new State();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialStates = _initialStates
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            agentState.avatarAddresses.Add(0, _avatarAddress);
            var inventoryAddr = _avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddr = _avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddr = _avatarAddress.Derive(LegacyQuestListKey);

            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                new PrivateKey().ToAddress(),
                "Avatar0"
            );

            _initialStates = _initialStates
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddr, avatarState.inventory.Serialize())
                .SetState(worldInformationAddr, avatarState.worldInformation.Serialize())
                .SetState(questListAddr, avatarState.questList.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize());

            for (var i = 0; i < GameConfig.SlotCount; i++)
            {
                var addr = CombinationSlotState.DeriveAddress(_avatarAddress, i);
                const int unlock = GameConfig.RequireClearedStageLevel.CombinationEquipmentAction;
                _initialStates = _initialStates.SetState(
                    addr,
                    new CombinationSlotState(addr, unlock).Serialize());
            }
        }

        [Fact]
        public void Execute_Success()
        {
            Execute(_initialStates, _avatarAddress, "Joy");
        }

        [Fact]
        public void Execute_Throw_AgentStateNotContainsAvatarAddressException()
        {
            var invalidAddr = new PrivateKey().ToAddress();
            Assert.Throws<AgentStateNotContainsAvatarAddressException>(() =>
                Execute(_initialStates, invalidAddr, "Joy"));
        }

        [Theory]
        [InlineData("J")]
        [InlineData("Joy!")]
        [InlineData("J o y")]
        public void Execute_Throw_InvalidNamePatternException(string name)
        {
            Assert.Throws<InvalidNamePatternException>(() =>
                Execute(_initialStates, _avatarAddress, name));
        }

        private void Execute(
            IAccountStateDelta previousStates,
            Address targetAvatarAddr,
            string name)
        {
            // Create action.
            var action = new ChangeAvatarName
            {
                TargetAvatarAddr = targetAvatarAddr,
                Name = name,
            };

            // Execute action.
            var nextStates = action.Execute(new ActionContext
            {
                PreviousStates = previousStates,
                Signer = _agentAddress,
                Rehearsal = false,
            });

            // Check next states.
            var avatarState = nextStates.GetAvatarState(_avatarAddress);
            Assert.Equal(targetAvatarAddr, avatarState.address);
            Assert.Equal(name, avatarState.name);
        }
    }
}
