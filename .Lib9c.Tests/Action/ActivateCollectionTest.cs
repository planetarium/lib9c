namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Collection;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class ActivateCollectionTest
    {
        private readonly IWorld _initialState;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;

        public ActivateCollectionTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            // Fix csv data for test
            sheets[nameof(CollectionSheet)] =
                @"id,item_id,count,level,skill,item_id,count,level,skill,item_id,count,level,skill,item_id,count,level,skill,item_id,count,level,skill,item_id,count,level,skill,stat_type,modify_type,modify_value,stat_type,modify_type,modify_value,stat_type,modify_type,modify_value
1,10110000,1,0,,302000,2,,,200000,2,,,40100000,1,,,,,,,,,,,ATK,Add,1,,,,,,
2,10110000,1,0,,,,,,,,,,,,,,,,,,,,,,ATK,Percentage,1,,,,,,";

            _tableSheets = new TableSheets(sheets);

            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.Address;
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            var avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            avatarState.level = 100;

            agentState.avatarAddresses.Add(0, _avatarAddress);

            _initialState = new World(MockUtil.MockModernWorldState)
                .SetAgentState(_agentAddress, agentState)
                .SetAvatarState(_avatarAddress, avatarState, true, true, true, true)
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Fact]
        public void Execute()
        {
            var row = _tableSheets.CollectionSheet.Values.First();
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var materials = new List<ICollectionMaterial>();
            var random = new TestRandom();
            foreach (var material in row.Materials)
            {
                var itemRow = _tableSheets.ItemSheet[material.ItemId];
                var itemType = itemRow.ItemType;
                if (itemType == ItemType.Material)
                {
                    var item = ItemFactory.CreateItem(itemRow, random);
                    avatarState.inventory.AddItem(item, material.Count);
                    materials.Add(new FungibleCollectionMaterial
                    {
                        ItemId = item.Id,
                        ItemCount = material.Count,
                    });
                }
                else
                {
                    for (var i = 0; i < material.Count; i++)
                    {
                        var item = ItemFactory.CreateItem(itemRow, random);
                        var nonFungibleId = ((INonFungibleItem)item).NonFungibleId;
                        avatarState.inventory.AddItem(item);
                        if (item.ItemType != ItemType.Consumable)
                        {
                            materials.Add(new NonFungibleCollectionMaterial
                            {
                                ItemId = item.Id,
                                NonFungibleId = nonFungibleId,
                                SkillContains = material.SkillContains,
                            });
                        }
                        else
                        {
                            // Add consumable material only one.
                            if (i == 0)
                            {
                                materials.Add(new FungibleCollectionMaterial
                                {
                                    ItemId = item.Id,
                                    ItemCount = material.Count,
                                });
                            }
                        }
                    }
                }
            }

            var state = _initialState.SetAvatarState(_avatarAddress, avatarState, false, true, false, false);
            IActionContext context = new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
            };
            var activateCollection = new ActivateCollection()
            {
                AvatarAddress = _avatarAddress,
                CollectionData =
                {
                    (row.Id, materials),
                },
            };

            var nextState = activateCollection.Execute(context);
            var collectionState = nextState.GetCollectionState(_avatarAddress);
            Assert.Equal(row.Id, collectionState.Ids.Single());

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.Empty(nextAvatarState.inventory.Items);

            Assert.Throws<AlreadyActivatedException>(() => activateCollection.Execute(new ActionContext
            {
                PreviousState = nextState,
                Signer = _agentAddress,
            }));
        }
    }
}
