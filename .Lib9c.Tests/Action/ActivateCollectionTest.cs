namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static SerializeKeys;

    public class ActivateCollectionTest
    {
        private readonly IAccount _initialState;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;

        public ActivateCollectionTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);

            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.Address;
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            )
            {
                level = 100,
            };
            var inventoryAddress = _avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = _avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = _avatarAddress.Derive(LegacyQuestListKey);
            agentState.avatarAddresses.Add(0, _avatarAddress);

            _initialState = new Account(MockState.Empty)
                .SetState(_agentAddress, agentState.SerializeV2())
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize());

            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Fact]
        public void Execute()
        {
            var row = _tableSheets.CollectionSheet.Values.First();
            var avatarState = _initialState.GetAvatarStateV2(_avatarAddress);
            var itemIdList = new List<Guid>();
            foreach (var material in row.Materials)
            {
                var itemRow = _tableSheets.ItemSheet[material.ItemId];
                for (int i = 0; i < material.Count; i++)
                {
                    var item = (ItemUsable)ItemFactory.CreateItem(itemRow, new TestRandom());
                    avatarState.inventory.AddItem(item);
                    itemIdList.Add(item.ItemId);
                }
            }

            var inventoryAddress = _avatarAddress.Derive(LegacyInventoryKey);
            var state = _initialState.SetState(inventoryAddress, avatarState.inventory.Serialize());
            IActionContext context = new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
            };
            ActivateCollection activateCollection = new ActivateCollection()
            {
                AvatarAddress = _avatarAddress,
                CollectionId = row.Id,
                ItemIdList = itemIdList,
            };

            var nextState = activateCollection.Execute(context);
            var collectionAddress = CollectionState.Derive(_avatarAddress);
            var rawList = Assert.IsType<List>(nextState.GetState(collectionAddress));
            var collectionState = new CollectionState(rawList);
            Assert.Equal(row.Id, collectionState.Ids.Single());

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            Assert.Empty(nextAvatarState.inventory.Items);
        }
    }
}
