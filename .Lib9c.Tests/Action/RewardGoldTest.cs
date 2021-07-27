namespace Lib9c.Tests.Action
{
    using System;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class RewardGoldTest
    {
        private readonly AvatarState _avatarState;
        private readonly State _baseState;
        private readonly TableSheets _tableSheets;

        public RewardGoldTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            sheets[nameof(CharacterSheet)] = string.Join(
                    Environment.NewLine,
                    "id,_name,size_type,elemental_type,hp,atk,def,cri,hit,spd,lv_hp,lv_atk,lv_def,lv_cri,lv_hit,lv_spd,attack_range,run_speed",
                    "100010,전사,S,0,300,20,10,10,90,70,12,0.8,0.4,0,3.6,2.8,2,3");

            var privateKey = new PrivateKey();
            var agentAddress = privateKey.PublicKey.ToAddress();

            var avatarAddress = agentAddress.Derive("avatar");
            _tableSheets = new TableSheets(sheets);

            _avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            var gold = new GoldCurrencyState(new Currency("NCG", 2, minter: null));
            _baseState = (State)new State()
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .SetState(Addresses.GoldDistribution, GoldDistributionTest.Fixture.Select(v => v.Serialize()).Serialize())
                .MintAsset(GoldCurrencyState.Address, gold.Currency * 100000000000);
        }

        [Fact]
        public void ExecuteCreateNextWeeklyArenaState()
        {
            var weekly = new WeeklyArenaState(0);
            var gameConfigState = new GameConfigState();
            gameConfigState.Set(_tableSheets.GameConfigSheet);
            var state = _baseState
                .SetState(weekly.address, weekly.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize());
            var action = new RewardGold();

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Miner = default,
                BlockIndex = 1,
            });

            Assert.Contains(WeeklyArenaState.DeriveAddress(1), nextState.UpdatedAddresses);
        }

        [Fact]
        public void ExecuteResetCount()
        {
            var weekly = new WeeklyArenaState(0);
            weekly.Set(_avatarState, _tableSheets.CharacterSheet);
            weekly[_avatarState.address].Update(_avatarState, weekly[_avatarState.address], BattleLog.Result.Lose);

            Assert.Equal(4, weekly[_avatarState.address].DailyChallengeCount);

            var gameConfigState = new GameConfigState();
            gameConfigState.Set(_tableSheets.GameConfigSheet);
            var state = _baseState
                .SetState(weekly.address, weekly.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize());
            var action = new RewardGold();

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Miner = default,
                BlockIndex = gameConfigState.DailyArenaInterval,
            });

            var current = nextState.GetWeeklyArenaState(0);

            Assert.Contains(WeeklyArenaState.DeriveAddress(1), nextState.UpdatedAddresses);
            Assert.Equal(gameConfigState.DailyArenaInterval, current.ResetIndex);
            Assert.Equal(5, current[_avatarState.address].DailyChallengeCount);
        }

        [Fact]
        public void ExecuteUpdateNextWeeklyArenaState()
        {
            var prevWeekly = new WeeklyArenaState(0);
            prevWeekly.Set(_avatarState, _tableSheets.CharacterSheet);
            prevWeekly[_avatarState.address].Activate();

            Assert.False(prevWeekly.Ended);
            Assert.True(prevWeekly[_avatarState.address].Active);

            var weekly = new WeeklyArenaState(1);
            var gameConfigState = new GameConfigState();
            gameConfigState.Set(_tableSheets.GameConfigSheet);
            var state = _baseState
                .SetState(prevWeekly.address, prevWeekly.Serialize())
                .SetState(weekly.address, weekly.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize());

            var action = new RewardGold();

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Miner = default,
                BlockIndex = gameConfigState.WeeklyArenaInterval,
            });

            var prev = nextState.GetWeeklyArenaState(0);
            var current = nextState.GetWeeklyArenaState(1);

            Assert.Equal(prevWeekly.address, prev.address);
            Assert.Equal(weekly.address, current.address);
            Assert.True(prev.Ended);
            Assert.Equal(gameConfigState.WeeklyArenaInterval, current.ResetIndex);
            Assert.Contains(_avatarState.address, current);
        }

        [Fact]
        public void GoldDistributedEachAccount()
        {
            Currency currency = new Currency("NCG", 2, minters: null);
            Address fund = GoldCurrencyState.Address;
            Address address1 = new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9");
            Address address2 = new Address("Fb90278C67f9b266eA309E6AE8463042f5461449");
            var action = new RewardGold();

            var ctx = new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _baseState,
            };

            IAccountStateDelta delta;

            // 제너시스에 받아야 할 돈들 검사
            delta = action.GenesisGoldDistribution(ctx, _baseState, currency);
            Assert.Equal(currency * 99999000000, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 1000000, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));

            // 1번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 1;
            delta = action.GenesisGoldDistribution(ctx, _baseState, currency);
            Assert.Equal(currency * 99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));

            // 3599번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 3599;
            delta = action.GenesisGoldDistribution(ctx, _baseState, currency);
            Assert.Equal(currency * 99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));

            // 3600번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 3600;
            delta = action.GenesisGoldDistribution(ctx, _baseState, currency);
            Assert.Equal(currency * 99999996900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 3000, delta.GetBalance(address2, currency));

            // 13600번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 13600;
            delta = action.GenesisGoldDistribution(ctx, _baseState, currency);
            Assert.Equal(currency * 99999996900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 3000, delta.GetBalance(address2, currency));

            // 13601번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 13601;
            delta = action.GenesisGoldDistribution(ctx, _baseState, currency);
            Assert.Equal(currency * 99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));

            // Fund 잔액을 초과해서 송금하는 경우
            // EndBlock이 긴 순서대로 송금을 진행하기 때문에, 100이 송금 성공하고 10억이 송금 실패한다.
            ctx.BlockIndex = 2;
            Assert.Throws<InsufficientBalanceException>(() =>
            {
                delta = action.GenesisGoldDistribution(ctx, _baseState, currency);
            });
            Assert.Equal(currency * 99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));
        }

        [Fact]
        public void MiningReward()
        {
            Address miner = new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9");
            Currency currency = _baseState.GetGoldCurrency();
            var ctx = new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _baseState,
                Miner = miner,
            };

            var action = new RewardGold();

            void AssertMinerReward(int blockIndex, string expected)
            {
                ctx.BlockIndex = blockIndex;
                IAccountStateDelta delta = action.MinerReward(ctx, _baseState, currency);
                Assert.Equal(FungibleAssetValue.Parse(currency, expected), delta.GetBalance(miner, currency));
            }

            // Before halving (10 / 2^0 = 10)
            AssertMinerReward(0, "10");
            AssertMinerReward(1, "10");
            AssertMinerReward(12614400, "10");

            // First halving (10 / 2^1 = 5)
            AssertMinerReward(12614401, "5");
            AssertMinerReward(25228800, "5");

            // Second halving (10 / 2^2 = 2.5)
            AssertMinerReward(25228801, "2.5");
            AssertMinerReward(37843200, "2.5");

            // Third halving (10 / 2^3 = 1.25)
            AssertMinerReward(37843201, "1.25");
            AssertMinerReward(50457600, "1.25");

            // Rewardless era
            AssertMinerReward(50457601, "0");
        }
    }
}
