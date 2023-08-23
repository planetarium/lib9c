namespace Lib9c.Tests.Action.Coupons
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Coupons;
    using Nekoyume.Action.Extensions;
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
            IWorld world = new MockWorld(
                new MockAccount()
                    .SetState(
                        Addresses.GameConfig,
                        new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize()
                    ));

            foreach (var (key, value) in sheets)
            {
                world = LegacyModule.SetState(world, Addresses.TableSheet.Derive(key), value.Serialize());
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

            var agent1Avatar0State = AvatarState.CreateAvatarState(
                    "agent1avatar0",
                    agent1Avatar0Address,
                    new ActionContext
                    {
                        PreviousState = world,
                        Signer = CouponsFixture.AgentAddress1,
                        BlockIndex = 0,
                    },
                    LegacyModule.GetSheet<MaterialItemSheet>(world),
                    default);
            var agent1Avatar1State = AvatarState.CreateAvatarState(
                    "agent1avatar1",
                    agent1Avatar1Address,
                    new ActionContext
                    {
                        PreviousState = world,
                        Signer = CouponsFixture.AgentAddress1,
                        BlockIndex = 0,
                    },
                    LegacyModule.GetSheet<MaterialItemSheet>(world),
                    default);
            var agent2Avatar0State = AvatarState.CreateAvatarState(
                    "agent2avatar0",
                    agent2Avatar0Address,
                    new ActionContext
                    {
                        PreviousState = world,
                        Signer = CouponsFixture.AgentAddress2,
                        BlockIndex = 0,
                    },
                    LegacyModule.GetSheet<MaterialItemSheet>(world),
                    default);

            world = AvatarModule.SetAvatarStateV2(world, agent1Avatar0Address, agent1Avatar0State);
            world = LegacyModule.SetState(
                    world,
                    agent1Avatar0Address.Derive(SerializeKeys.LegacyInventoryKey),
                    agent1Avatar0State.inventory.Serialize());
            world = LegacyModule.SetState(
                world,
                agent1Avatar0Address.Derive(SerializeKeys.LegacyWorldInformationKey),
                agent1Avatar0State.worldInformation.Serialize());
            world = LegacyModule.SetState(
                world,
                agent1Avatar0Address.Derive(SerializeKeys.LegacyQuestListKey),
                agent1Avatar0State.questList.Serialize());
            world = AvatarModule.SetAvatarStateV2(world, agent1Avatar1Address, agent1Avatar1State);
            world = LegacyModule.SetState(
                world,
                agent1Avatar1Address.Derive(SerializeKeys.LegacyInventoryKey),
                agent1Avatar1State.inventory.Serialize());
            world = LegacyModule.SetState(
                world,
                agent1Avatar1Address.Derive(SerializeKeys.LegacyWorldInformationKey),
                agent1Avatar1State.worldInformation.Serialize());
            world = LegacyModule.SetState(
                world,
                agent1Avatar1Address.Derive(SerializeKeys.LegacyQuestListKey),
                agent1Avatar1State.questList.Serialize());
            world = AvatarModule.SetAvatarStateV2(world, agent2Avatar0Address, agent2Avatar0State);
            world = LegacyModule.SetState(
                world,
                agent2Avatar0Address.Derive(SerializeKeys.LegacyInventoryKey),
                agent2Avatar0State.inventory.Serialize());
            world = LegacyModule.SetState(
                world,
                agent2Avatar0Address.Derive(SerializeKeys.LegacyWorldInformationKey),
                agent2Avatar0State.worldInformation.Serialize());
            world = LegacyModule.SetState(
                world,
                agent2Avatar0Address.Derive(SerializeKeys.LegacyQuestListKey),
                agent2Avatar0State.questList.Serialize());

            // can't redeem a coupon with an arbitrary guid
            // FIXME: This should not world because IWorld does not implement IEquals interface.
            Assert.Equal(
                world,
                new RedeemCoupon(
                    new Guid("AEB63B38-1850-4003-B549-19D37B37AC89"),
                    agent1Avatar0Address)
                    .Execute(
                    new ActionContext
                    {
                        PreviousState = world,
                        Rehearsal = false,
                        Signer = CouponsFixture.AgentAddress1,
                        Random = random,
                    }));

            var agent1CouponWallet = LegacyModule.GetCouponWallet(world, CouponsFixture.AgentAddress1);
            var agent2CouponWallet = LegacyModule.GetCouponWallet(world, CouponsFixture.AgentAddress2);

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

            world = LegacyModule.SetCouponWallet(world, CouponsFixture.AgentAddress1, agent1CouponWallet);
            world = LegacyModule.SetCouponWallet(world, CouponsFixture.AgentAddress2, agent2CouponWallet);

            var rehearsedWorld = new RedeemCoupon(CouponsFixture.Guid1, agent1Avatar0Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = world,
                        Rehearsal = true,
                        Signer = CouponsFixture.AgentAddress1,
                        Random = random,
                    });

            Assert.Equal(
                ActionBase.MarkChanged,
                LegacyModule.GetState(rehearsedWorld, agent1Avatar0Address));

            Assert.Equal(
                ActionBase.MarkChanged,
                LegacyModule.GetState(
                    rehearsedWorld,
                    agent1Avatar0Address.Derive(SerializeKeys.LegacyInventoryKey)));

            Assert.Equal(
                ActionBase.MarkChanged,
                LegacyModule.GetState(
                    rehearsedWorld,
                    agent1Avatar0Address.Derive(SerializeKeys.LegacyWorldInformationKey)));

            Assert.Equal(
                ActionBase.MarkChanged,
                LegacyModule.GetState(
                    rehearsedWorld,
                    agent1Avatar0Address.Derive(SerializeKeys.LegacyQuestListKey)));

            Assert.Equal(
                ActionBase.MarkChanged,
                LegacyModule.GetState(
                    rehearsedWorld,
                    CouponsFixture.AgentAddress1.Derive(SerializeKeys.CouponWalletKey)));

            // can't redeem other person's coupon
            var expected = AvatarModule.GetAvatarStateV2(world, agent1Avatar0Address);
            world = new RedeemCoupon(CouponsFixture.Guid3, agent1Avatar0Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = world,
                        Rehearsal = false,
                        Signer = CouponsFixture.AgentAddress1,
                        Random = random,
                    });
            Assert.Equal(
                expected.SerializeV2(),
                AvatarModule.GetAvatarStateV2(world, agent1Avatar0Address).SerializeV2());
            Assert.Equal(
                agent2CouponWallet,
                LegacyModule.GetCouponWallet(world, CouponsFixture.AgentAddress2));

            // can't redeem other person's coupon to their account
            expected = AvatarModule.GetAvatarStateV2(world, agent2Avatar0Address);
            world = new RedeemCoupon(CouponsFixture.Guid3, agent2Avatar0Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = world,
                        Rehearsal = false,
                        Signer = CouponsFixture.AgentAddress1,
                        Random = random,
                    });
            Assert.Equal(
                expected.SerializeV2(),
                AvatarModule.GetAvatarStateV2(world, agent2Avatar0Address).SerializeV2());
            Assert.Equal(agent2CouponWallet, LegacyModule.GetCouponWallet(world, CouponsFixture.AgentAddress2));

            // can't redeem to a nonexistent avatar
            world = new RedeemCoupon(
                    CouponsFixture.Guid1,
                    CouponsFixture.AgentAddress1
                        .Derive(SerializeKeys.AvatarAddressKey)
                        .Derive("avatar-states-2"))
                .Execute(
                    new ActionContext
                    {
                        PreviousState = world,
                        Rehearsal = false,
                        Signer = CouponsFixture.AgentAddress1,
                        Random = random,
                    });
            Assert.Null(
                AvatarModule.GetAvatarStateV2(
                    world,
                    CouponsFixture.AgentAddress1
                        .Derive(SerializeKeys.AvatarAddressKey)
                        .Derive("avatar-states-2")));
            Assert.Equal(agent1CouponWallet, LegacyModule.GetCouponWallet(world, CouponsFixture.AgentAddress1));

            expected = AvatarModule.GetAvatarStateV2(world, agent2Avatar0Address);
            // can't redeem to an avatar of different agent
            world = new RedeemCoupon(
                    CouponsFixture.Guid1,
                    agent2Avatar0Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = world,
                        Rehearsal = false,
                        Signer = CouponsFixture.AgentAddress1,
                        Random = random,
                    });
            Assert.Equal(
                expected.SerializeV2(),
                AvatarModule.GetAvatarStateV2(world, agent2Avatar0Address).SerializeV2());
            Assert.Equal(agent1CouponWallet, LegacyModule.GetCouponWallet(world, CouponsFixture.AgentAddress1));

            // redeem a coupon
            expected = AvatarModule.GetAvatarStateV2(world, agent1Avatar0Address);
            world = new RedeemCoupon(
                    CouponsFixture.Guid1,
                    agent1Avatar0Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = world,
                        Rehearsal = false,
                        Signer = CouponsFixture.AgentAddress1,
                        Random = random,
                    });
            var actual = AvatarModule.GetAvatarStateV2(world, agent1Avatar0Address);
            Assert.Equal(
                CouponsFixture.RewardSet1.Select(kv => kv.Key).ToImmutableSortedSet(),
                actual.inventory.Items
                    .Select(item => item.item.Id)
                    .ToImmutableSortedSet());
            expected.inventory = actual.inventory;
            Assert.Equal(expected.SerializeV2(), actual.SerializeV2());

            // can't redeem a coupon twice
            expected = AvatarModule.GetAvatarStateV2(world, agent1Avatar1Address);
            world = new RedeemCoupon(
                    CouponsFixture.Guid1,
                    agent1Avatar1Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = world,
                        Rehearsal = false,
                        Signer = CouponsFixture.AgentAddress1,
                        Random = random,
                    });
            actual = AvatarModule.GetAvatarStateV2(world, agent1Avatar1Address);
            Assert.Equal(0, AvatarModule.GetAvatarStateV2(world, agent1Avatar1Address).inventory.Items.Count);
            Assert.Empty(actual.inventory.Items);
            Assert.Equal(expected.SerializeV2(), actual.SerializeV2());

            expected = AvatarModule.GetAvatarStateV2(world, agent1Avatar1Address);
            world = new RedeemCoupon(
                    CouponsFixture.Guid2,
                    agent1Avatar1Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = world,
                        Rehearsal = false,
                        Signer = CouponsFixture.AgentAddress1,
                        Random = random,
                    });
            actual = AvatarModule.GetAvatarStateV2(world, agent1Avatar1Address);
            Assert.Equal(
                CouponsFixture.RewardSet2.Select(kv => kv.Key).ToImmutableSortedSet(),
                actual.inventory.Items
                    .Select(item => item.item.Id)
                    .ToImmutableSortedSet());
            Assert.Equal(expected.SerializeV2(), actual.SerializeV2());

            expected = AvatarModule.GetAvatarStateV2(world, agent2Avatar0Address);
            world = new RedeemCoupon(
                    CouponsFixture.Guid3,
                    agent2Avatar0Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = world,
                        Rehearsal = false,
                        Signer = CouponsFixture.AgentAddress2,
                        Random = random,
                    });
            actual = AvatarModule.GetAvatarStateV2(world, agent2Avatar0Address);
            Assert.Equal(
                CouponsFixture.RewardSet3.Select(kv => kv.Key).ToImmutableSortedSet(),
                actual.inventory.Items
                    .Select(item => item.item.Id)
                    .ToImmutableSortedSet());
            Assert.Equal(expected.SerializeV2(), actual.SerializeV2());

            world = LegacyModule.SetCouponWallet(world, CouponsFixture.AgentAddress1, agent1CouponWallet);
            expected = AvatarModule.GetAvatarStateV2(world, agent1Avatar0Address);
            world = new RedeemCoupon(
                    CouponsFixture.Guid2,
                    agent1Avatar0Address)
                .Execute(
                    new ActionContext
                    {
                        PreviousState = world,
                        Rehearsal = false,
                        Signer = CouponsFixture.AgentAddress1,
                        Random = random,
                    });
            actual = AvatarModule.GetAvatarStateV2(world, agent1Avatar0Address);
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
            Assert.Equal(expected.SerializeV2(), actual.SerializeV2());
        }
    }
}
