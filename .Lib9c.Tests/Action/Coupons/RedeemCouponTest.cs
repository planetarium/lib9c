namespace Lib9c.Tests.Action.Coupons
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Coupons;
    using Nekoyume.Model.Coupons;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class RedeemCouponTest
    {
        [Fact]
        public void Execute()
        {
            IRandom random = new TestRandom();
            var sheets = TableSheetsImporter.ImportSheets();
            var state = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(
                    Addresses.GameConfig,
                    new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize()
                );

            foreach (var (key, value) in sheets)
            {
                state = state.SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var agent1Avatar0Address = CouponsFixture.AgentAddress1
                .Derive(SerializeKeys.AvatarAddressKey)
                .Derive("avatar-states-0");
            var agent1Avatar1Address = CouponsFixture.AgentAddress1
                .Derive(SerializeKeys.AvatarAddressKey)
                .Derive("avatar-states-1");
            var agent2Avatar0Address = CouponsFixture.AgentAddress2
                .Derive(SerializeKeys.AvatarAddressKey)
                .Derive("avatar-states-0");

            var agent1Avatar0State = CreateAvatar.CreateAvatarState(
                "agent1avatar0",
                agent1Avatar0Address,
                new ActionContext
                {
                    PreviousState = state,
                    Signer = CouponsFixture.AgentAddress1,
                    BlockIndex = 0,
                },
                state.GetSheet<MaterialItemSheet>(),
                default);
            var agent1Avatar1State = CreateAvatar.CreateAvatarState(
                "agent1avatar1",
                agent1Avatar1Address,
                new ActionContext
                {
                    PreviousState = state,
                    Signer = CouponsFixture.AgentAddress1,
                    BlockIndex = 0,
                },
                state.GetSheet<MaterialItemSheet>(),
                default);
            var agent2Avatar0State = CreateAvatar.CreateAvatarState(
                "agent2avatar0",
                agent2Avatar0Address,
                new ActionContext
                {
                    PreviousState = state,
                    Signer = CouponsFixture.AgentAddress2,
                    BlockIndex = 0,
                },
                state.GetSheet<MaterialItemSheet>(),
                default);

            state = state
                .SetAvatarState(agent1Avatar0Address, agent1Avatar0State)
                .SetAvatarState(agent1Avatar1Address, agent1Avatar1State)
                .SetAvatarState(agent2Avatar0Address, agent2Avatar0State);

            // can't redeem a coupon with an arbitrary guid
            Assert.Equal(
                state,
                new RedeemCoupon(
                        new Guid("AEB63B38-1850-4003-B549-19D37B37AC89"),
                        agent1Avatar0Address)
                    .Execute(
                        new ActionContext
                        {
                            PreviousState = state,
                            Signer = CouponsFixture.AgentAddress1,
                            RandomSeed = random.Seed,
                        }));

            var agent1CouponWallet = state.GetCouponWallet(CouponsFixture.AgentAddress1);
            var agent2CouponWallet = state.GetCouponWallet(CouponsFixture.AgentAddress2);

            agent1CouponWallet = agent1CouponWallet
                .Add(
                    CouponsFixture.Guid1,
                    new Coupon(CouponsFixture.Guid1, CouponsFixture.RewardSet1))
                .Add(
                    CouponsFixture.Guid2,
                    new Coupon(CouponsFixture.Guid2, CouponsFixture.RewardSet2));
            agent2CouponWallet = agent2CouponWallet
                .Add(
                    CouponsFixture.Guid3,
                    new Coupon(CouponsFixture.Guid3, CouponsFixture.RewardSet3));

            state = state
                .SetCouponWallet(CouponsFixture.AgentAddress1, agent1CouponWallet)
                .SetCouponWallet(CouponsFixture.AgentAddress2, agent2CouponWallet);

            // can't redeem other person's coupon
            var expected = state.GetAvatarState(agent1Avatar0Address);
            state = new RedeemCoupon(CouponsFixture.Guid3, agent1Avatar0Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress1,
                        RandomSeed = random.Seed,
                    });
            Assert.Equal(
                expected.SerializeList(),
                state.GetAvatarState(agent1Avatar0Address).SerializeList());
            Assert.Equal(agent2CouponWallet, state.GetCouponWallet(CouponsFixture.AgentAddress2));

            // can't redeem other person's coupon to their account
            expected = state.GetAvatarState(agent2Avatar0Address);
            state = new RedeemCoupon(CouponsFixture.Guid3, agent2Avatar0Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress1,
                        RandomSeed = random.Seed,
                    });
            Assert.Equal(
                expected.SerializeList(),
                state.GetAvatarState(agent2Avatar0Address).SerializeList());
            Assert.Equal(agent2CouponWallet, state.GetCouponWallet(CouponsFixture.AgentAddress2));

            // can't redeem to a nonexistent avatar
            state = new RedeemCoupon(
                    CouponsFixture.Guid1,
                    CouponsFixture.AgentAddress1
                        .Derive(SerializeKeys.AvatarAddressKey)
                        .Derive("avatar-states-2"))
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress1,
                        RandomSeed = random.Seed,
                    });
            Assert.Null(
                state.GetAvatarState(
                    CouponsFixture.AgentAddress1
                        .Derive(SerializeKeys.AvatarAddressKey)
                        .Derive("avatar-states-2")));
            Assert.Equal(agent1CouponWallet, state.GetCouponWallet(CouponsFixture.AgentAddress1));

            expected = state.GetAvatarState(agent2Avatar0Address);
            // can't redeem to an avatar of different agent
            state = new RedeemCoupon(
                    CouponsFixture.Guid1,
                    agent2Avatar0Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress1,
                        RandomSeed = random.Seed,
                    });
            Assert.Equal(
                expected.SerializeList(),
                state.GetAvatarState(agent2Avatar0Address).SerializeList());
            Assert.Equal(agent1CouponWallet, state.GetCouponWallet(CouponsFixture.AgentAddress1));

            // redeem a coupon
            expected = state.GetAvatarState(agent1Avatar0Address);
            state = new RedeemCoupon(
                    CouponsFixture.Guid1,
                    agent1Avatar0Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress1,
                        RandomSeed = random.Seed,
                    });
            var actual = state.GetAvatarState(agent1Avatar0Address);
            Assert.Equal(
                CouponsFixture.RewardSet1.Select(kv => kv.Key).ToImmutableSortedSet(),
                actual.inventory.Items
                    .Select(item => item.item.Id)
                    .ToImmutableSortedSet());
            expected.inventory = actual.inventory;
            Assert.Equal(expected.SerializeList(), actual.SerializeList());

            // can't redeem a coupon twice
            expected = state.GetAvatarState(agent1Avatar1Address);
            state = new RedeemCoupon(
                    CouponsFixture.Guid1,
                    agent1Avatar1Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress1,
                        RandomSeed = random.Seed,
                    });
            actual = state.GetAvatarState(agent1Avatar1Address);
            Assert.Equal(0, state.GetAvatarState(agent1Avatar1Address).inventory.Items.Count);
            Assert.Empty(actual.inventory.Items);
            Assert.Equal(expected.SerializeList(), actual.SerializeList());

            expected = state.GetAvatarState(agent1Avatar1Address);
            state = new RedeemCoupon(
                    CouponsFixture.Guid2,
                    agent1Avatar1Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress1,
                        RandomSeed = random.Seed,
                    });
            actual = state.GetAvatarState(agent1Avatar1Address);
            Assert.Equal(
                CouponsFixture.RewardSet2.Select(kv => kv.Key).ToImmutableSortedSet(),
                actual.inventory.Items
                    .Select(item => item.item.Id)
                    .ToImmutableSortedSet());
            Assert.Equal(expected.SerializeList(), actual.SerializeList());

            expected = state.GetAvatarState(agent2Avatar0Address);
            state = new RedeemCoupon(
                    CouponsFixture.Guid3,
                    agent2Avatar0Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress2,
                        RandomSeed = random.Seed,
                    });
            actual = state.GetAvatarState(agent2Avatar0Address);
            Assert.Equal(
                CouponsFixture.RewardSet3.Select(kv => kv.Key).ToImmutableSortedSet(),
                actual.inventory.Items
                    .Select(item => item.item.Id)
                    .ToImmutableSortedSet());
            Assert.Equal(expected.SerializeList(), actual.SerializeList());

            state = state
                .SetCouponWallet(CouponsFixture.AgentAddress1, agent1CouponWallet);
            expected = state.GetAvatarState(agent1Avatar0Address);
            state = new RedeemCoupon(
                    CouponsFixture.Guid2,
                    agent1Avatar0Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = state,
                        Signer = CouponsFixture.AgentAddress1,
                        RandomSeed = random.Seed,
                    });
            actual = state.GetAvatarState(agent1Avatar0Address);
            var aggregateRewardSet = CouponsFixture.RewardSet1.Aggregate(
                CouponsFixture.RewardSet2, (rewardSet, kv) =>
                {
                    if (rewardSet.TryGetValue(kv.Key, out var val))
                    {
                        rewardSet = new RewardSet(
                            rewardSet.ToImmutableDictionary().SetItem(kv.Key, val + kv.Value));
                    }
                    else
                    {
                        rewardSet = new RewardSet(
                            rewardSet.ToImmutableDictionary().SetItem(kv.Key, kv.Value));
                    }

                    return rewardSet;
                });
            Assert.Equal(
                aggregateRewardSet.Select(kv => kv.Key).ToImmutableSortedSet(),
                actual.inventory.Items
                    .Select(item => item.item.Id)
                    .ToImmutableSortedSet());
            foreach (var kv in aggregateRewardSet)
            {
                var item = actual.inventory.Items.Single(item => item.item.Id.Equals(kv.Key));
                Assert.Equal((int)kv.Value, item.count);
            }

            expected.inventory = actual.inventory;
            Assert.Equal(expected.SerializeList(), actual.SerializeList());
        }
    }
}
