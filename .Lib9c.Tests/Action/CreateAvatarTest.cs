namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Libplanet;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class CreateAvatarTest
    {
        private readonly Address _agentAddress;
        private readonly TableSheets _tableSheets;
        private readonly Currency _currency;

        public CreateAvatarTest()
        {
            _agentAddress = default;
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _currency = Currency.Legacy("NCG", 2, minters: null);
        }

        [Fact]
        public void Execute()
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
            var state = new State()
                .SetState(
                    Addresses.GameConfig,
                    new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize()
                )
                .SetState(Addresses.GoldCurrency, new GoldCurrencyState(_currency).Serialize());

            foreach (var (key, value) in sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            Assert.Equal(0 * CrystalCalculator.CRYSTAL, state.GetBalance(_agentAddress, CrystalCalculator.CRYSTAL));

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                BlockIndex = 0,
                Random = new TestRandom(),
            });

            var avatarAddress = _agentAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CreateAvatar2.DeriveFormat,
                    0
                )
            );
            Assert.True(nextState.TryGetAgentAvatarStatesV2(
                default,
                avatarAddress,
                out var agentState,
                out var nextAvatarState,
                out _)
            );
            Assert.True(agentState.avatarAddresses.Any());
            Assert.Equal("test", nextAvatarState.name);
            Assert.Equal(1_000_000_000 * CrystalCalculator.CRYSTAL, nextState.GetBalance(_agentAddress, CrystalCalculator.CRYSTAL));

            // internal test
            Assert.Equal(60, nextAvatarState.inventory.Equipments.Count());
            Assert.All(nextAvatarState.inventory.Equipments, equipment => Assert.Equal(0, equipment.level));
            Assert.Equal(290, nextAvatarState.level);
            var player = new Player(nextAvatarState, _tableSheets.CharacterSheet, _tableSheets.CharacterLevelSheet, _tableSheets.EquipmentItemSetEffectSheet);
            Assert.Equal(290, player.Level);
            player.GetExp(0);
            Assert.Equal(290, player.Level);
            var equipmentList = nextAvatarState.inventory.Equipments.ToList();
            nextAvatarState.ValidateItemRequirement(
                equipmentList.Select(e => e.Id).ToList(),
                equipmentList,
                _tableSheets.ItemRequirementSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheetV2,
                _tableSheets.EquipmentItemOptionSheet,
                string.Empty);
            var soulStones = new List<(int id, int count)>
            {
                // Soulstone_1001
                (1001, 2_500),
                // Soulstone_1002
                (1002, 2_500),
                // Solustone_1003
                (1003, 8_000),
                // Solustone_1004
                (1004, 8_000),
            };
            var petSheet = _tableSheets.PetSheet;
            foreach (var values in soulStones)
            {
                var row = petSheet[values.id];
                var soulStoneCurrency = Currency.Legacy(
                    row.SoulStoneTicker,
                    0,
                    minters: null);
                var soulStone = values.count * soulStoneCurrency;
                Assert.Equal(soulStone, nextState.GetBalance(avatarAddress, soulStoneCurrency));
            }

            Assert.Equal(50_000 * _currency, nextState.GetBalance(_agentAddress, _currency));
            Assert.True(nextAvatarState.worldInformation.IsWorldUnlocked(5));
            Assert.True(nextAvatarState.worldInformation.IsStageCleared(250));
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

            var state = new State();

            Assert.Throws<InvalidNamePatternException>(() => action.Execute(new ActionContext()
                {
                    PreviousStates = state,
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
                    CreateAvatar2.DeriveFormat,
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

            var state = new State().SetState(avatarAddress, avatarState.Serialize());

            Assert.Throws<InvalidAddressException>(() => action.Execute(new ActionContext()
                {
                    PreviousStates = state,
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
            var state = new State().SetState(_agentAddress, agentState.Serialize());
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
                    PreviousStates = state,
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
                    CreateAvatar2.DeriveFormat,
                    0
                )
            );
            agentState.avatarAddresses[index] = avatarAddress;
            var state = new State().SetState(_agentAddress, agentState.Serialize());

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
                    PreviousStates = state,
                    Signer = _agentAddress,
                    BlockIndex = 0,
                })
            );
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Rehearsal(int index)
        {
            var agentAddress = default(Address);
            var avatarAddress = _agentAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CreateAvatar2.DeriveFormat,
                    index
                )
            );

            var action = new CreateAvatar()
            {
                index = index,
                hair = 0,
                ear = 0,
                lens = 0,
                tail = 0,
                name = "test",
            };

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var gold = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618
            var updatedAddresses = new List<Address>()
            {
                agentAddress,
                avatarAddress,
                avatarAddress.Derive(LegacyInventoryKey),
                avatarAddress.Derive(LegacyQuestListKey),
                avatarAddress.Derive(LegacyWorldInformationKey),
            };
            for (var i = 0; i < AvatarState.CombinationSlotCapacity; i++)
            {
                var slotAddress = avatarAddress.Derive(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CombinationSlotState.DeriveFormat,
                        i
                    )
                );
                updatedAddresses.Add(slotAddress);
            }

            var state = new State();

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = agentAddress,
                BlockIndex = 0,
                Rehearsal = true,
            });

            Assert.Equal(
                updatedAddresses.ToImmutableHashSet(),
                nextState.UpdatedAddresses
            );
        }

        [Fact]
        public void Serialize_With_DotnetAPI()
        {
            var formatter = new BinaryFormatter();
            var action = new CreateAvatar()
            {
                index = 2,
                hair = 1,
                ear = 4,
                lens = 5,
                tail = 7,
                name = "test",
            };

            using var ms = new MemoryStream();
            formatter.Serialize(ms, action);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (CreateAvatar)formatter.Deserialize(ms);

            Assert.Equal(2, deserialized.index);
            Assert.Equal(1, deserialized.hair);
            Assert.Equal(4, deserialized.ear);
            Assert.Equal(5, deserialized.lens);
            Assert.Equal(7, deserialized.tail);
            Assert.Equal("test", deserialized.name);
        }
    }
}
