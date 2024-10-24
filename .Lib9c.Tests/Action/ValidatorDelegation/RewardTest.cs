#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Linq;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Action.ValidatorDelegation;
using Xunit;

public class RewardTest : TxAcitonTestBase
{
    [Fact]
    public void Execute()
    {
        // Given
        var signerKey = new PrivateKey();
        var recipientKey = new PrivateKey();
        var signerMead = Mead * 5;
        EnsureToMintAsset(signerKey, signerMead);
        EnsureToMintAsset(signerKey, NCG * 100);

        // When
        var amount = NCG * 1;
        var gasLimit = 4;
        var expectedNCG = amount;
        var expectedMead = signerMead - Mead * gasLimit;
        var transferAsset = new TransferAsset(
            signerKey.Address, recipientKey.Address, amount, memo: "test");
        MakeTransaction(
            signerKey,
            new ActionBase[] { transferAsset, },
            maxGasPrice: Mead * 1,
            gasLimit: gasLimit);
        MoveToNextBlock(throwOnError: true);

        // Then
        var actualNCG = GetBalance(recipientKey.Address, NCG);
        var actualMead = GetBalance(signerKey.Address, Mead);
        Assert.Equal(expectedNCG, actualNCG);
        Assert.Equal(expectedMead, actualMead);
    }

    [Fact]
    public void Execute_InsufficientMead_Throw()
    {
        // Given
        var signerKey = new PrivateKey();
        var recipientKey = new PrivateKey();
        var signerMead = Mead * 3;
        EnsureToMintAsset(signerKey, signerMead);
        EnsureToMintAsset(signerKey, NCG * 100);

        // When
        var amount = NCG * 1;
        var gasLimit = 4;
        var expectedNCG = NCG * 0;
        var expectedMead = signerMead;
        var transferAsset = new TransferAsset(
            signerKey.Address, recipientKey.Address, amount, memo: "test");
        MakeTransaction(
            signerKey,
            new ActionBase[] { transferAsset, },
            maxGasPrice: Mead * 1,
            gasLimit: gasLimit);

        // Then
        var e = Assert.Throws<AggregateException>(() => MoveToNextBlock(throwOnError: true));
        var innerExceptions = e.InnerExceptions
            .Cast<UnexpectedlyTerminatedActionException>()
            .ToArray();
        var actualNCG = GetBalance(recipientKey.Address, NCG);
        var actualMead = GetBalance(signerKey.Address, Mead);
        Assert.Contains(innerExceptions, i => i.Action is Reward);
        Assert.Equal(expectedNCG, actualNCG);
        Assert.Equal(expectedMead, actualMead);
    }
}
