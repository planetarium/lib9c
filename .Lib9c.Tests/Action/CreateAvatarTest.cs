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
    using Nekoyume.Action.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Exceptions;
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
        [InlineData(0L, 600_000)]
        [InlineData(7_210_000L, 600_000)]
        [InlineData(7_210_001L, 200_000)]
        public void Execute(long blockIndex, int expected)
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
            IWorld state = new MockWorld();
            state = LegacyModule.SetState(
                state,
                Addresses.GameConfig,
                new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize());

            foreach (var (key, value) in sheets)
            {
                state = LegacyModule.SetState(
                    state,
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            Assert.Equal(
                0 * CrystalCalculator.CRYSTAL,
                LegacyModule.GetBalance(state, _agentAddress, CrystalCalculator.CRYSTAL));

            var nextWorld = action.Execute(new ActionContext()
            {
                PreviousState = state,
                Signer = _agentAddress,
                BlockIndex = blockIndex,
            });

            var nextAccount = nextWorld.GetAccount(ReservedAddresses.LegacyAccount);

            var avatarAddress = _agentAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CreateAvatar.DeriveFormat,
                    0
                )
            );
            var agentState = AgentModule.GetAgentState(nextWorld, default);
            Assert.NotNull(agentState);
            Assert.True(AvatarModule.TryGetAvatarState(
                nextWorld,
                default,
                avatarAddress,
                out var nextAvatarState)
            );
            Assert.True(agentState.avatarAddresses.Any());
            Assert.Equal("test", nextAvatarState.name);
            Assert.Equal(expected * CrystalCalculator.CRYSTAL, nextAccount.GetBalance(_agentAddress, CrystalCalculator.CRYSTAL));
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

            Assert.Throws<InvalidNamePatternException>(() => action.Execute(new ActionContext()
                {
                    PreviousState = new MockWorld(),
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

            IWorld state = new MockWorld();
            state = AvatarModule.SetAvatarState(
                state,
                avatarAddress,
                avatarState,
                true,
                true,
                true,
                true);

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
            IWorld state = new MockWorld();
            state = AgentModule.SetAgentState(state, _agentAddress, agentState);
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
            IWorld state = new MockWorld();
            state = AgentModule.SetAgentState(state, _agentAddress, agentState);

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
                    CreateAvatar.DeriveFormat,
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

            var updatedAddressesAvatar = new List<Address>()
            {
                avatarAddress,
            };
            var updatedAddressesLegacy = new List<Address>()
            {
                agentAddress,
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
                updatedAddressesLegacy.Add(slotAddress);
            }

            var nextState = action.Execute(new ActionContext()
            {
                PreviousState = new MockWorld(),
                Signer = agentAddress,
                BlockIndex = 0,
                Rehearsal = true,
            });
            Assert.Equal(
                updatedAddressesAvatar.ToImmutableHashSet(),
                nextState.GetAccount(Addresses.Avatar).Delta.UpdatedAddresses
            );
            Assert.Equal(
                updatedAddressesLegacy.ToImmutableHashSet(),
                nextState.GetAccount(ReservedAddresses.LegacyAccount).Delta.UpdatedAddresses
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
