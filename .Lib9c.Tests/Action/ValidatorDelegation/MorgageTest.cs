#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Linq;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Action.Guild;
using Nekoyume.Action.ValidatorDelegation;
using Xunit;

public class MorgageTest : TxAcitonTestBase
{
    [Fact]
    public void Execute()
    {
        // Given
        var signerKey = new PrivateKey();
        var signerMead = Mead * 10;
        var validatorKey = ValidatorKey;
        EnsureToMintAsset(signerKey, signerMead);

        // When
        var expectedMead = signerMead - (Mead * 1);
        var actions = new IAction[] { new MakeGuild(validatorKey.Address), };
        MakeTransaction(
            signerKey, actions, maxGasPrice: Mead * 1, gasLimit: 1);
        MoveToNextBlock();

        // Then
        var actualMead = GetBalance(signerKey.Address, Mead);
        Assert.Equal(expectedMead, actualMead);
    }

    [Fact]
    public void Execute_WithoutMead_Throw()
    {
        // Given
        var signerKey = new PrivateKey();
        var signerMead = Mead * 10;
        var validatorKey = ValidatorKey;

        // When
        var actions = new IAction[] { new MakeGuild(validatorKey.Address), };
        MakeTransaction(
            signerKey, actions, maxGasPrice: Mead * 1, gasLimit: 1);

        // Then
        var e = Assert.Throws<AggregateException>(() => MoveToNextBlock(throwOnError: true));
        var innerExceptions = e.InnerExceptions
            .Cast<UnexpectedlyTerminatedActionException>()
            .ToArray();
        Assert.Contains(innerExceptions, i => i.Action is Mortgage);
    }

    [Fact]
    public void Execute_InsufficientMead_Throw()
    {
        // Given
        var signerKey = new PrivateKey();
        var recipientKey = new PrivateKey();
        var signerMead = Mead * 1;
        var validatorKey = ValidatorKey;
        EnsureToMintAsset(signerKey, signerMead);
        EnsureToMintAsset(signerKey, NCG * 100);

        // When
        var transferAsset = new TransferAsset(
            signerKey.Address, recipientKey.Address, Mead * 1, memo: "test");
        MakeTransaction(
            signerKey,
            new ActionBase[] { transferAsset, },
            maxGasPrice: Mead * 1,
            gasLimit: 1);

        // Then
        var e = Assert.Throws<AggregateException>(() => MoveToNextBlock(throwOnError: true));
        var innerExceptions = e.InnerExceptions
            .Cast<UnexpectedlyTerminatedActionException>()
            .ToArray();
        Assert.Contains(innerExceptions, i => i.Action is Mortgage);
    }
}
