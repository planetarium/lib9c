namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class UnlockRuneSlotTest
    {
        private readonly Currency _goldCurrency;

        public UnlockRuneSlotTest()
        {
            _goldCurrency = Currency.Legacy("NCG", 2, null);
        }

        public IAccount Init(out Address agentAddress, out Address avatarAddress, out long blockIndex)
        {
            agentAddress = new PrivateKey().Address;
            avatarAddress = new PrivateKey().Address;
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            blockIndex = tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var state = new Account(MockState.Empty)
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(agentAddress, new AgentState(agentAddress).Serialize());

            foreach (var (key, value) in sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
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
            return state.SetState(gameConfigState.address, gameConfigState.Serialize());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        public void Execute(int slotIndex)
        {
            var context = new ActionContext();
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var gameConfig = state.GetGameConfigState();
            var cost = slotIndex == 1
                ? gameConfig.RuneStatSlotUnlockCost
                : gameConfig.RuneSkillSlotUnlockCost;
            var ncgCurrency = state.GetGoldCurrency();
            state = state.MintAsset(context, agentAddress, cost * ncgCurrency);
            var action = new UnlockRuneSlot()
            {
                AvatarAddress = avatarAddress,
                SlotIndex = slotIndex,
            };

            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = state,
                RandomSeed = 0,
                Signer = agentAddress,
            };

            state = action.Execute(ctx);
            var adventureAddr = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Adventure);
            if (state.TryGetState(adventureAddr, out List adventureRaw))
            {
                var s = new RuneSlotState(adventureRaw);
                var slot = s.GetRuneSlot().FirstOrDefault(x => x.Index == slotIndex);
                Assert.NotNull(slot);
                Assert.False(slot.IsLock);
            }

            var arenaAddr = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Arena);
            if (state.TryGetState(arenaAddr, out List arenaRaw))
            {
                var s = new RuneSlotState(arenaRaw);
                var slot = s.GetRuneSlot().FirstOrDefault(x => x.Index == slotIndex);
                Assert.NotNull(slot);
                Assert.False(slot.IsLock);
            }

            var raidAddr = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Raid);
            if (state.TryGetState(raidAddr, out List raidRaw))
            {
                var s = new RuneSlotState(raidRaw);
                var slot = s.GetRuneSlot().FirstOrDefault(x => x.Index == slotIndex);
                Assert.NotNull(slot);
                Assert.False(slot.IsLock);
            }

            var balance = state.GetBalance(agentAddress, ncgCurrency);
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
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var gameConfig = state.GetGameConfigState();
            var ncgCurrency = state.GetGoldCurrency();
            state = state.MintAsset(context, agentAddress, gameConfig.RuneStatSlotUnlockCost * ncgCurrency);
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
                Signer = agentAddress,
            };

            state = action.Execute(ctx);

            Assert.Throws<SlotIsAlreadyUnlockedException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousState = state,
                    Signer = agentAddress,
                    RandomSeed = 0,
                    BlockIndex = blockIndex,
                }));
        }

        [Theory]
        [InlineData(true, 6)]
        [InlineData(false, 6)]
        [InlineData(true, 7)]
        [InlineData(false, 7)]
        public void Execute_CRYSTAL(bool legacyState, int slotIndex)
        {
            var context = new ActionContext();
            var state = Init(out var agentAddress, out var avatarAddress, out var blockIndex);
            var gameConfig = state.GetGameConfigState();
            var cost = slotIndex == 6
                ? gameConfig.RuneStatSlotCrystalUnlockCost
                : gameConfig.RuneSkillSlotCrystalUnlockCost;
            state = state.MintAsset(context, agentAddress, cost * Currencies.Crystal);
            if (legacyState)
            {
                foreach (var battleType in new[] { BattleType.Adventure, BattleType.Arena, BattleType.Raid })
                {
                    var runeSlotState = new RuneSlotState(battleType);
                    var serialized = (List)runeSlotState.Serialize();
                    var rawSlots = new List(((List)serialized[1]).Take(6));
                    state = state.SetState(
                        RuneSlotState.DeriveAddress(avatarAddress, battleType),
                        List.Empty.Add(battleType.Serialize()).Add(rawSlots));
                }
            }

            var action = new UnlockRuneSlot()
            {
                AvatarAddress = avatarAddress,
                SlotIndex = slotIndex,
            };

            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = state,
                RandomSeed = 0,
                Signer = agentAddress,
            };

            state = action.Execute(ctx);
            var adventureAddr = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Adventure);
            if (state.TryGetState(adventureAddr, out List adventureRaw))
            {
                var s = new RuneSlotState(adventureRaw);
                var slot = s.GetRuneSlot().FirstOrDefault(x => x.Index == slotIndex);
                Assert.NotNull(slot);
                Assert.False(slot.IsLock);
            }

            var arenaAddr = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Arena);
            if (state.TryGetState(arenaAddr, out List arenaRaw))
            {
                var s = new RuneSlotState(arenaRaw);
                var slot = s.GetRuneSlot().FirstOrDefault(x => x.Index == slotIndex);
                Assert.NotNull(slot);
                Assert.False(slot.IsLock);
            }

            var raidAddr = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Raid);
            if (state.TryGetState(raidAddr, out List raidRaw))
            {
                var s = new RuneSlotState(raidRaw);
                var slot = s.GetRuneSlot().FirstOrDefault(x => x.Index == slotIndex);
                Assert.NotNull(slot);
                Assert.False(slot.IsLock);
            }

            var balance = state.GetBalance(agentAddress, Currencies.Crystal);
            Assert.Equal("0", balance.GetQuantityString());
        }
    }
}
