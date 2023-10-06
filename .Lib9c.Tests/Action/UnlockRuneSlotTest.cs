namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class UnlockRuneSlotTest
    {
        private readonly Currency _goldCurrency;

        public UnlockRuneSlotTest()
        {
            _goldCurrency = Currency.Legacy("NCG", 2, null);
        }

        public IWorld Init(out Address agentAddress, out Address avatarAddress, out long blockIndex)
        {
            agentAddress = new PrivateKey().ToAddress();
            avatarAddress = new PrivateKey().ToAddress();
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            blockIndex = tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            IWorld state = new MockWorld();
            state = LegacyModule.SetState(state, goldCurrencyState.address, goldCurrencyState.Serialize());
            state = AgentModule.SetAgentState(state, agentAddress, new AgentState(agentAddress));

            foreach (var (key, value) in sheets)
            {
                state = LegacyModule.SetState(state, Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            );
            state = LegacyModule.SetState(state, gameConfigState.address, gameConfigState.Serialize());
            return new MockWorld(state);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        public void Execute(int slotIndex)
        {
            var context = new ActionContext();
            var world = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var account = world.GetAccount(ReservedAddresses.LegacyAccount);
            var gameConfig = LegacyModule.GetGameConfigState(world);
            var cost = slotIndex == 1
                ? gameConfig.RuneStatSlotUnlockCost
                : gameConfig.RuneSkillSlotUnlockCost;
            var ncgCurrency = LegacyModule.GetGoldCurrency(world);
            account = account.MintAsset(context, agentAddress, cost * ncgCurrency);
            world = world.SetAccount(ReservedAddresses.LegacyAccount, account);
            var action = new UnlockRuneSlot()
            {
                AvatarAddress = avatarAddress,
                SlotIndex = slotIndex,
            };

            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = world,
                RandomSeed = 0,
                Rehearsal = false,
                Signer = agentAddress,
            };

            world = action.Execute(ctx);
            account = world.GetAccount(ReservedAddresses.LegacyAccount);
            var adventureAddr = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Adventure);
            if (LegacyModule.TryGetState(world, adventureAddr, out List adventureRaw))
            {
                var s = new RuneSlotState(adventureRaw);
                var slot = s.GetRuneSlot().FirstOrDefault(x => x.Index == slotIndex);
                Assert.NotNull(slot);
                Assert.False(slot.IsLock);
            }

            var arenaAddr = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Arena);
            if (LegacyModule.TryGetState(world, arenaAddr, out List arenaRaw))
            {
                var s = new RuneSlotState(arenaRaw);
                var slot = s.GetRuneSlot().FirstOrDefault(x => x.Index == slotIndex);
                Assert.NotNull(slot);
                Assert.False(slot.IsLock);
            }

            var raidAddr = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Raid);
            if (LegacyModule.TryGetState(world, raidAddr, out List raidRaw))
            {
                var s = new RuneSlotState(raidRaw);
                var slot = s.GetRuneSlot().FirstOrDefault(x => x.Index == slotIndex);
                Assert.NotNull(slot);
                Assert.False(slot.IsLock);
            }

            var balance = account.GetBalance(agentAddress, ncgCurrency);
            Assert.Equal("0", balance.GetQuantityString());
        }

        [Fact]
        public void Execute_InsufficientBalanceException()
        {
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var action = new UnlockRuneSlot()
            {
                AvatarAddress = avatarAddress,
                SlotIndex = 1,
            };

            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = state,
                RandomSeed = 0,
                Rehearsal = false,
                Signer = agentAddress,
            };

            Assert.Throws<InsufficientBalanceException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = agentAddress,
                    RandomSeed = 0,
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_SlotNotFoundException()
        {
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var action = new UnlockRuneSlot()
            {
                AvatarAddress = avatarAddress,
                SlotIndex = 99,
            };

            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = state,
                RandomSeed = 0,
                Rehearsal = false,
                Signer = agentAddress,
            };

            Assert.Throws<SlotNotFoundException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = agentAddress,
                    RandomSeed = 0,
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_MismatchRuneSlotTypeException()
        {
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var action = new UnlockRuneSlot()
            {
                AvatarAddress = avatarAddress,
                SlotIndex = 0,
            };

            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = state,
                RandomSeed = 0,
                Rehearsal = false,
                Signer = agentAddress,
            };

            Assert.Throws<MismatchRuneSlotTypeException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = agentAddress,
                    RandomSeed = 0,
                    BlockIndex = blockIndex,
                }));
        }

        [Fact]
        public void Execute_SlotIsAlreadyUnlockedException()
        {
            var context = new ActionContext();
            var world = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var account = world.GetAccount(ReservedAddresses.LegacyAccount);
            var gameConfig = LegacyModule.GetGameConfigState(world);
            var ncgCurrency = LegacyModule.GetGoldCurrency(world);
            account = account.MintAsset(context, agentAddress, gameConfig.RuneStatSlotUnlockCost * ncgCurrency);
            world = world.SetAccount(ReservedAddresses.LegacyAccount, account);
            var action = new UnlockRuneSlot()
            {
                AvatarAddress = avatarAddress,
                SlotIndex = 1,
            };

            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = world,
                RandomSeed = 0,
                Rehearsal = false,
                Signer = agentAddress,
            };

            world = action.Execute(ctx);

            Assert.Throws<SlotIsAlreadyUnlockedException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousState = world,
                    Signer = agentAddress,
                    RandomSeed = 0,
                    BlockIndex = blockIndex,
                }));
        }
    }
}
