namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Immutable;
    using System.Globalization;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.Coupons;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class AccountStateDeltaExtensionsTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AgentState _agentState;
        private readonly AvatarState _avatarState;
        private readonly TableSheets _tableSheets;

        public AccountStateDeltaExtensionsTest()
        {
            _agentAddress = default;
            _avatarAddress = _agentAddress.Derive(string.Format(CultureInfo.InvariantCulture, CreateAvatar.DeriveFormat, 0));
            _agentState = new AgentState(_agentAddress);
            _agentState.avatarAddresses[0] = _avatarAddress;
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _avatarState = AvatarState.Create(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
        }

        [Theory]
        [InlineData(0, 0, 0, typeof(InvalidClaimException))]
        [InlineData(1, 1, 100, null)]
        [InlineData(2, 2, 200, null)]
        public void SetWorldBossKillReward(int level, int expectedRune, int expectedCrystal, Type exc)
        {
            var context = new ActionContext();
            IWorld states = new World(MockUtil.MockModernWorldState);
            var rewardInfoAddress = new PrivateKey().Address;
            var rewardRecord = new WorldBossKillRewardRecord();
            for (var i = 0; i < level; i++)
            {
                rewardRecord[i] = false;
            }

            states = states.SetLegacyState(rewardInfoAddress, rewardRecord.Serialize());

            var random = new TestRandom();
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var runeSheet = tableSheets.RuneSheet;
            var materialItemSheet = tableSheets.MaterialItemSheet;
            var runeCurrency = RuneHelper.ToCurrency(runeSheet[10001]);
            var bossState = new WorldBossState(
                tableSheets.WorldBossListSheet[1],
                tableSheets.WorldBossGlobalHpSheet[1]
            );
            var bossId = bossState.Id;
            var runeWeightSheet = new RuneWeightSheet();
            runeWeightSheet.Set(
                $@"id,boss_id,rank,rune_id,weight
1,{bossId},0,10001,100
");
            var killRewardSheet = new WorldBossKillRewardSheet();
            killRewardSheet.Set(
                $@"id,boss_id,rank,rune_min,rune_max,crystal,circle
1,{bossId},0,1,1,100,0
");

            if (exc is null)
            {
                var nextState = states.SetWorldBossKillReward(
                    context,
                    rewardInfoAddress,
                    rewardRecord,
                    0,
                    bossState,
                    runeWeightSheet,
                    killRewardSheet,
                    runeSheet,
                    materialItemSheet,
                    random,
                    _avatarState.inventory,
                    _avatarAddress,
                    _agentAddress);
                Assert.Equal(expectedRune * runeCurrency, nextState.GetBalance(_avatarState.address, runeCurrency));
                Assert.Equal(expectedCrystal * CrystalCalculator.CRYSTAL, nextState.GetBalance(_agentAddress, CrystalCalculator.CRYSTAL));
                var nextRewardInfo = new WorldBossKillRewardRecord((List)nextState.GetLegacyState(rewardInfoAddress));
                Assert.All(nextRewardInfo, kv => Assert.True(kv.Value));
            }
            else
            {
                Assert.Throws(
                    exc,
                    () => states.SetWorldBossKillReward(
                        context,
                        rewardInfoAddress,
                        rewardRecord,
                        0,
                        bossState,
                        runeWeightSheet,
                        killRewardSheet,
                        runeSheet,
                        materialItemSheet,
                        random,
                        _avatarState.inventory,
                        _avatarAddress,
                        _agentAddress)
                );
            }
        }

        [Fact]
        public void SetCouponWallet()
        {
            IWorld states = new World(MockUtil.MockModernWorldState);
            var guid1 = new Guid("6856AE42-A820-4041-92B0-5D7BAA52F2AA");
            var guid2 = new Guid("701BA698-CCB9-4FC7-B88F-7CB8C707D135");
            var guid3 = new Guid("910296E7-34E4-45D7-9B4E-778ED61F278B");
            var coupon1 = new Coupon(guid1, (1, 2));
            var coupon2 = new Coupon(guid2, (1, 2), (3, 4));
            var coupon3 = new Coupon(guid3, (3, 4));
            var agentAddress1 = new Address("0000000000000000000000000000000000000000");
            var agentAddress2 = new Address("0000000000000000000000000000000000000001");

            states = states.SetCouponWallet(
                agentAddress2,
                ImmutableDictionary<Guid, Coupon>.Empty);

            Assert.Null(
                states.GetLegacyState(agentAddress1.Derive(SerializeKeys.CouponWalletKey)));
            Assert.Equal(
                Bencodex.Types.List.Empty,
                states.GetLegacyState(agentAddress2.Derive(SerializeKeys.CouponWalletKey)));

            states = states.SetCouponWallet(
                agentAddress1,
                ImmutableDictionary<Guid, Coupon>.Empty
                    .Add(guid1, coupon1)
                    .Add(guid2, coupon2));

            states = states.SetCouponWallet(
                agentAddress2,
                ImmutableDictionary<Guid, Coupon>.Empty
                    .Add(guid3, coupon3));

            Assert.Equal(
                Bencodex.Types.List.Empty
                    .Add(coupon1.Serialize())
                    .Add(coupon2.Serialize()),
                states.GetLegacyState(agentAddress1.Derive(SerializeKeys.CouponWalletKey)));

            Assert.Equal(
                Bencodex.Types.List.Empty
                    .Add(coupon3.Serialize()),
                states.GetLegacyState(agentAddress2.Derive(SerializeKeys.CouponWalletKey)));
        }
    }
}
