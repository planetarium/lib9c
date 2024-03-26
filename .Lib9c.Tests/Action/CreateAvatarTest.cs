namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class CreateAvatarTest
    {
        private readonly Address _agentAddress;
        private readonly TableSheets _tableSheets;

        public CreateAvatarTest()
        {
            _agentAddress = default;
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(7_210_000L)]
        [InlineData(7_210_001L)]
        public void Execute(long blockIndex)
        {
            var action = new CreateAvatar()
            {
                index = 0,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = "test",
            };

            var sheets = TableSheetsImporter.ImportSheets();
            var state = new World(new MockWorldState())
                .SetLegacyState(
                    Addresses.GameConfig,
                    new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize()
                );

            foreach (var (key, value) in sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            Assert.Equal(0 * CrystalCalculator.CRYSTAL, state.GetBalance(_agentAddress, CrystalCalculator.CRYSTAL));

            var nextState = action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
                BlockIndex = blockIndex,
                RandomSeed = 0,
            });

            var avatarAddress = _agentAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CreateAvatar.DeriveFormat,
                    0
                )
            );
            Assert.True(nextState.TryGetAvatarState(
                default,
                avatarAddress,
                out var nextAvatarState)
            );
            var agentState = nextState.GetAgentState(default);
            Assert.NotNull(agentState);
            Assert.True(agentState.avatarAddresses.Any());
            Assert.Equal("test", nextAvatarState.name);
            Assert.Equal(200_000 * CrystalCalculator.CRYSTAL, nextState.GetBalance(_agentAddress, CrystalCalculator.CRYSTAL));
            var avatarItemSheet = nextState.GetSheet<CreateAvatarItemSheet>();
            foreach (var row in avatarItemSheet.Values)
            {
                Assert.True(nextAvatarState.inventory.HasItem(row.ItemId, row.Count));
            }

            var avatarFavSheet = nextState.GetSheet<CreateAvatarFavSheet>();
            foreach (var row in avatarFavSheet.Values)
            {
                var targetAddress = row.Target == CreateAvatarFavSheet.Target.Agent
                    ? _agentAddress
                    : avatarAddress;
                Assert.Equal(row.Currency * row.Quantity, nextState.GetBalance(targetAddress, row.Currency));
            }
        }

        [Theory]
        [InlineData("홍길동")]
        [InlineData("山田太郎")]
        public void ExecuteThrowInvalidNamePatterException(string nickName)
        {
            var agentAddress = default(Address);

            var action = new CreateAvatar()
            {
                index = 0,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = nickName,
            };

            var state = new World(new MockWorldState());

            Assert.Throws<InvalidNamePatternException>(() => action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = agentAddress,
                    BlockIndex = 0,
                })
            );
        }

        [Fact]
        public void ExecuteThrowInvalidAddressException()
        {
            var avatarAddress = _agentAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CreateAvatar.DeriveFormat,
                    0
                )
            );

            var avatarState = new AvatarState(
                avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            var action = new CreateAvatar()
            {
                index = 0,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = "test",
            };

            var state = new World(new MockWorldState()).SetAvatarState(avatarAddress, avatarState);

            Assert.Throws<InvalidAddressException>(() => action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    BlockIndex = 0,
                })
            );
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public void ExecuteThrowAvatarIndexOutOfRangeException(int index)
        {
            var agentState = new AgentState(_agentAddress);
            var state = new World(new MockWorldState()).SetAgentState(_agentAddress, agentState);
            var action = new CreateAvatar()
            {
                index = index,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = "test",
            };

            Assert.Throws<AvatarIndexOutOfRangeException>(() => action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    BlockIndex = 0,
                })
            );
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ExecuteThrowAvatarIndexAlreadyUsedException(int index)
        {
            var agentState = new AgentState(_agentAddress);
            var avatarAddress = _agentAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CreateAvatar.DeriveFormat,
                    0
                )
            );
            agentState.avatarAddresses[index] = avatarAddress;
            var state = new World(new MockWorldState()).SetAgentState(_agentAddress, agentState);

            var action = new CreateAvatar()
            {
                index = index,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = "test",
            };

            Assert.Throws<AvatarIndexAlreadyUsedException>(() => action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    BlockIndex = 0,
                })
            );
        }

        [Fact]
        public void AddItem()
        {
            var itemSheet = _tableSheets.ItemSheet;
            var createAvatarItemSheet = new CreateAvatarItemSheet();
            createAvatarItemSheet.Set(@"item_id,count
10112000,2
10512000,2
600201,2
");
            var avatarState = new AvatarState(default, default, 0L, _tableSheets.GetAvatarSheets(), new GameConfigState(), default, "test");
            CreateAvatar.AddItem(itemSheet, createAvatarItemSheet, avatarState, new TestRandom());
            foreach (var row in createAvatarItemSheet.Values)
            {
                Assert.True(avatarState.inventory.HasItem(row.ItemId, row.Count));
            }

            Assert.Equal(4, avatarState.inventory.Equipments.Count());
            foreach (var equipment in avatarState.inventory.Equipments)
            {
                var equipmentRow = _tableSheets.EquipmentItemSheet[equipment.Id];
                Assert.Equal(equipmentRow.Stat, equipment.Stat);
            }
        }

        [Fact]
        public void MintAsset()
        {
            var createAvatarFavSheet = new CreateAvatarFavSheet();
            createAvatarFavSheet.Set(@"currency,quantity,target
CRYSTAL,200000,Agent
RUNE_GOLDENLEAF,200000,Avatar
");
            var avatarAddress = new PrivateKey().Address;
            var agentAddress = new PrivateKey().Address;
            var avatarState = new AvatarState(avatarAddress, agentAddress, 0L, _tableSheets.GetAvatarSheets(), new GameConfigState(), default, "test");
            var nextState = CreateAvatar.MintAsset(createAvatarFavSheet, avatarState, new World(new MockWorldState()), new ActionContext());
            foreach (var row in createAvatarFavSheet.Values)
            {
                var targetAddress = row.Target == CreateAvatarFavSheet.Target.Agent
                    ? agentAddress
                    : avatarAddress;
                Assert.Equal(row.Currency * row.Quantity, nextState.GetBalance(targetAddress, row.Currency));
            }
        }
    }
}
