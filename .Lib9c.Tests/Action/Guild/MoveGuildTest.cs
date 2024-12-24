namespace Lib9c.Tests.Action.Guild;

using System;
using Lib9c.Tests.Util;
using Libplanet.Crypto;
using Nekoyume.Action.Guild;
using Nekoyume.Model.Guild;
using Nekoyume.ValidatorDelegation;
using Xunit;

public class MoveGuildTest : GuildTestBase
{
    [Fact]
    public void Serialization()
    {
        var guildAddress = AddressUtil.CreateGuildAddress();
        var action = new MoveGuild(guildAddress);
        var plainValue = action.PlainValue;

        var deserialized = new MoveGuild();
        deserialized.LoadPlainValue(plainValue);
        Assert.Equal(guildAddress, deserialized.GuildAddress);
    }

    [Fact]
    public void Execute()
    {
        // Given
        var world = World;
        var validatorKey1 = new PrivateKey();
        var validatorKey2 = new PrivateKey();
        var agentAddress = AddressUtil.CreateAgentAddress();
        var guildMasterAddress1 = AddressUtil.CreateAgentAddress();
        var guildMasterAddress2 = AddressUtil.CreateAgentAddress();
        var guildAddress1 = AddressUtil.CreateGuildAddress();
        var guildAddress2 = AddressUtil.CreateGuildAddress();
        world = EnsureToCreateValidator(world, validatorKey1.PublicKey);
        world = EnsureToCreateValidator(world, validatorKey2.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress1, guildMasterAddress1, validatorKey1.Address);
        world = EnsureToMakeGuild(world, guildAddress2, guildMasterAddress2, validatorKey2.Address);
        world = EnsureToPrepareGuildGold(world, agentAddress, GG * 100);
        world = EnsureToJoinGuild(world, guildAddress1, agentAddress, 1L);

        // When
        var moveGuild = new MoveGuild(guildAddress2);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = agentAddress,
            BlockIndex = 2L,
        };
        world = moveGuild.Execute(actionContext);

        // Then
        var guildRepository = new GuildRepository(world, actionContext);
        var validatorRepository = new ValidatorRepository(world, actionContext);
        var guildDelegatee1 = guildRepository.GetDelegatee(validatorKey1.Address);
        var guildDelegatee2 = guildRepository.GetDelegatee(validatorKey2.Address);
        var validatorDelegatee1 = validatorRepository.GetDelegatee(validatorKey1.Address);
        var validatorDelegatee2 = validatorRepository.GetDelegatee(validatorKey2.Address);

        var guildParticipant = guildRepository.GetGuildParticipant(agentAddress);

        Assert.Equal(guildAddress2, guildParticipant.GuildAddress);
        Assert.Equal(0, guildDelegatee1.TotalShares);
        Assert.Equal(0, validatorDelegatee1.TotalShares);
        Assert.Equal(100 * SharePerGG, guildDelegatee2.TotalShares);
        Assert.Equal(100 * SharePerGG, validatorDelegatee2.TotalShares);
    }

    [Fact]
    public void Execute_ToGuildDelegatingToTombstonedValidator_Throw()
    {
        // Given
        var world = World;
        var validatorKey1 = new PrivateKey();
        var validatorKey2 = new PrivateKey();
        var agentAddress = AddressUtil.CreateAgentAddress();
        var guildMasterAddress1 = AddressUtil.CreateAgentAddress();
        var guildMasterAddress2 = AddressUtil.CreateAgentAddress();
        var guildAddress1 = AddressUtil.CreateGuildAddress();
        var guildAddress2 = AddressUtil.CreateGuildAddress();
        world = EnsureToCreateValidator(world, validatorKey1.PublicKey);
        world = EnsureToCreateValidator(world, validatorKey2.PublicKey);
        world = EnsureToMakeGuild(world, guildAddress1, guildMasterAddress1, validatorKey1.Address);
        world = EnsureToMakeGuild(world, guildAddress2, guildMasterAddress2, validatorKey2.Address);
        world = EnsureToPrepareGuildGold(world, agentAddress, GG * 100);
        world = EnsureToJoinGuild(world, guildAddress1, agentAddress, 1L);
        world = EnsureToTombstoneValidator(world, validatorKey2.Address);

        // When
        var moveGuild = new MoveGuild(guildAddress2);
        var actionContext = new ActionContext
        {
            PreviousState = world,
            Signer = agentAddress,
            BlockIndex = 2L,
        };

        // Then
        var exception = Assert.Throws<InvalidOperationException>(
            () => moveGuild.Execute(actionContext));
        Assert.Equal(
            "The validator of the guild to move to has been tombstoned.", exception.Message);
    }
}
