namespace Lib9c.Tests.Action.Guild;

using System;
using System.Collections.Generic;
using System.Numerics;
using Lib9c.Tests.Util;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Guild;
using Nekoyume.Model.Stake;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class ClaimUnbonded_ValidatorTest : GuildTestBase
{
    private interface IClaimUnbondedFixture
    {
        public PrivateKey ValidatorKey { get; }

        public FungibleAssetValue ValidatorNCG { get; }

        public BigInteger ShareToUndelegate { get; }

        public GuildAddress GuildAddress { get; }

        public BigInteger SlashFactor { get; }

        public AgentAddress MasterAddress { get; }

        public FungibleAssetValue MasterNCG { get; }
    }

    public static IEnumerable<object[]> RandomSeeds => new List<object[]>
    {
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
        new object[] { Random.Shared.Next() },
    };

    [Fact]
    public void Serialization()
    {
        var action = new ClaimUnbonded();
        var plainValue = action.PlainValue;

        var deserialized = new ClaimUnbonded();
        deserialized.LoadPlainValue(plainValue);
    }

    [Fact]
    public void Execute()
    {
        var fixture = new StaticFixture
        {
            ValidatorNCG = NCG * 100,
            SlashFactor = 0,
            MasterNCG = NCG * 100,
        };

        ExecuteWithFixture(fixture);
    }

    [Fact]
    public void Execute_SlashedValidator()
    {
        var fixture = new StaticFixture
        {
            ValidatorNCG = NCG * 200,
            SlashFactor = 10,
            ShareToUndelegate = (GG * 100).RawValue,
            MasterNCG = NCG * 100,
        };

        ExecuteWithFixture(fixture);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1181126949)]
    [InlineData(793705868)]
    [InlineData(559431555)]
    public void Execute_Fact_WithStaticSeed(int randomSeed)
    {
        var fixture = new RandomFixture(randomSeed);
        ExecuteWithFixture(fixture);
    }

    [Theory]
    [MemberData(nameof(RandomSeeds))]
    public void Execute_Fact_WithRandomSeed(int randomSeed)
    {
        var fixture = new RandomFixture(randomSeed);
        ExecuteWithFixture(fixture);
    }

    private void ExecuteWithFixture(IClaimUnbondedFixture fixture)
    {
        var world = World;
        var validatorKey = fixture.ValidatorKey;
        var validatorNCG = fixture.ValidatorNCG;
        var validatorGG = NCGToGG(validatorNCG);
        var shareToUndelegate = fixture.ShareToUndelegate;
        var stateStateAddress = StakeState.DeriveAddress(validatorKey.Address);
        var masterAddress = fixture.MasterAddress;
        var masterNCG = fixture.MasterNCG;
        var masterGG = NCGToGG(masterNCG);
        var guildAddress = fixture.GuildAddress;
        var height = 0L;
        var slashFactor = fixture.SlashFactor;
        world = EnsureToInitializeValidator(world, validatorKey, validatorNCG, height++);
        if (slashFactor > 1)
        {
            world = EnsureToSlashValidator(world, validatorKey, slashFactor, height++);
        }

        world = EnsureToInitializeAgent(world, masterAddress, masterNCG, height++);
        world = EnsureToMakeGuild(world, guildAddress, masterAddress, validatorKey, height++);
        if (masterNCG.Sign > 0)
        {
            world = EnsureToStake(world, masterAddress, masterNCG, height++);
        }

        world = EnsureToUndelegateValidator(world, validatorKey, shareToUndelegate, height++);

        // When
        var validatorSlashedGG = SlashFAV(slashFactor, validatorGG);
        var validatorShare = validatorGG.RawValue;
        var totalGG = validatorSlashedGG + masterGG;
        var masterShare = validatorShare * masterGG.RawValue / validatorSlashedGG.RawValue;
        var totalShare = validatorShare + masterShare;
        var validatorSlashedNCG = GGToNCG(validatorSlashedGG);
        var expectedGG = (totalGG * shareToUndelegate).DivRem(totalShare).Quotient;

        var claimUnbonded = new ClaimUnbonded();
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = validatorKey.Address,
            BlockIndex = height + ValidatorDelegatee.ValidatorUnbondingPeriod,
        };
        world = claimUnbonded.Execute(actionContext);

        // Then
        var actualGG = world.GetBalance(stateStateAddress, GG);
        Assert.Equal(expectedGG, actualGG);

        // Check ncg after unstaking
        var amountGG = validatorSlashedGG - expectedGG;
        const long minimumStakeAmount = 50;
        if (amountGG.MajorUnit >= minimumStakeAmount)
        {
            var ncg = GGToNCG(amountGG);
            var majorUnit = ncg.MinorUnit > 0 ? ncg.MajorUnit + 1 : ncg.MajorUnit;
            var amount = new FungibleAssetValue(NCG, majorUnit, 0);
            var expectedNCG = validatorSlashedNCG - amount;
            world = EnsureToStakeValidator(world, validatorKey, amount, height++);
            var actualNCG = world.GetBalance(validatorKey.Address, NCG);
            Assert.Equal(expectedNCG, actualNCG);
        }
    }

    private class StaticFixture : IClaimUnbondedFixture
    {
        public PrivateKey ValidatorKey { get; set; } = new PrivateKey();

        public FungibleAssetValue ValidatorNCG { get; set; } = NCG * 100;

        public BigInteger SlashFactor { get; set; }

        public BigInteger ShareToUndelegate { get; set; } = (GG * 50).RawValue;

        public GuildAddress GuildAddress { get; set; } = AddressUtil.CreateGuildAddress();

        public AgentAddress MasterAddress { get; set; } = AddressUtil.CreateAgentAddress();

        public FungibleAssetValue MasterNCG { get; set; } = NCG * 100;
    }

    private class RandomFixture : IClaimUnbondedFixture
    {
        private readonly Random _random;

        public RandomFixture(int randomSeed)
        {
            _random = new Random(randomSeed);
            ValidatorKey = GetRandomKey(_random);
            ValidatorNCG = GetRandomNCG(_random);
            SlashFactor = GetRandomSlashFactor(_random);
            ShareToUndelegate
                = NCGToGG(ValidatorNCG).DivRem(_random.Next(1, 100)).Quotient.RawValue;
            GuildAddress = GetRandomGuildAddress(_random);
            MasterAddress = GetRandomAgentAddress(_random);
            MasterNCG = GetRandomNCG(_random);
        }

        public PrivateKey ValidatorKey { get; }

        public FungibleAssetValue ValidatorNCG { get; }

        public BigInteger SlashFactor { get; }

        public BigInteger ShareToUndelegate { get; }

        public GuildAddress GuildAddress { get; }

        public AgentAddress MasterAddress { get; }

        public FungibleAssetValue MasterNCG { get; }
    }
}
