#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Linq;
using Lib9c.Action;
using Libplanet.Action;
using Libplanet.Crypto;
using Xunit;

public class GasWithTransferAssetTest : TxAcitonTestBase
{
    public const long GasConsumption = 4;

    [Theory]
    [InlineData(4, 4)]
    [InlineData(4, 5)]
    [InlineData(4, 6)]
    public void Execute(long gasLimit, long gasOwned)
    {
        if (gasLimit < GasConsumption)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gasLimit),
                $"{nameof(gasLimit)} must be greater than or equal to {GasConsumption}.");
        }

        if (gasOwned < GasConsumption)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gasOwned),
                $"{nameof(gasOwned)} must be greater than or equal to {GasConsumption}.");
        }

        // Given
        var signerKey = new PrivateKey();
        var signerMead = Mead * gasOwned;
        var recipientKey = new PrivateKey();
        EnsureToMintAsset(signerKey, signerMead);
        EnsureToMintAsset(signerKey, NCG * 100);

        // When
        var amount = NCG * 1;
        var expectedNCG = NCG * 1;
        var expectedMead = signerMead - Mead * GasConsumption;
        var transferAsset = new TransferAsset(
            signerKey.Address, recipientKey.Address, amount, memo: "test");
        MakeTransaction(
            signerKey,
            new ActionBase[] { transferAsset, },
            maxGasPrice: Mead * 1,
            gasLimit: gasLimit);

        // `
        MoveToNextBlock(throwOnError: true);
        var actualNCG = GetBalance(recipientKey.Address, NCG);
        var actualMead = GetBalance(signerKey.Address, Mead);
        Assert.Equal(expectedNCG, actualNCG);
        Assert.Equal(expectedMead, actualMead);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(1, 4)]
    [InlineData(4, 4)]
    public void Execute_Without_GasLimit_And_MaxGasPrice(
        long gasConsumption, long gasOwned)
    {
        if (gasOwned < gasConsumption)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gasOwned),
                $"{nameof(gasOwned)} must be greater than or equal to {nameof(gasConsumption)}.");
        }

        // Given
        var signerKey = new PrivateKey();
        var signerMead = Mead * gasOwned;
        var recipientKey = new PrivateKey();
        EnsureToMintAsset(signerKey, signerMead);
        EnsureToMintAsset(signerKey, NCG * 100);

        // When
        var amount = NCG * 1;
        var expectedNCG = NCG * 1;
        var expectedMead = signerMead;
        var transferAsset = new TransferAsset(
            signerKey.Address, recipientKey.Address, amount, memo: "test");
        MakeTransaction(
            signerKey,
            new ActionBase[] { transferAsset, });

        // Then
        MoveToNextBlock(throwOnError: true);
        var actualNCG = GetBalance(recipientKey.Address, NCG);
        var actualMead = GetBalance(signerKey.Address, Mead);
        Assert.Equal(expectedNCG, actualNCG);
        Assert.Equal(expectedMead, actualMead);
    }

    [Theory]
    [InlineData(4, 0)]
    [InlineData(5, 0)]
    [InlineData(6, 1)]
    public void Execute_InsufficientMead_Throw(
        long gasLimit, long gasOwned)
    {
        if (gasLimit < GasConsumption)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gasLimit),
                $"{nameof(gasLimit)} must be greater than or equal to {GasConsumption}.");
        }

        if (gasOwned >= GasConsumption)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gasOwned),
                $"{nameof(gasOwned)} must be less than {GasConsumption}.");
        }

        // Given
        var signerKey = new PrivateKey();
        var signerMead = Mead * gasOwned;
        var recipientKey = new PrivateKey();
        EnsureToMintAsset(signerKey, signerMead);
        EnsureToMintAsset(signerKey, NCG * 100);

        // When
        var amount = NCG * 1;
        var expectedMead = Mead * 0;
        var expectedNCG = NCG * 0;
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
        Assert.Single(innerExceptions);
        Assert.Contains(innerExceptions, i => i.Action is TransferAsset);
        Assert.Equal(expectedNCG, actualNCG);
        Assert.Equal(expectedMead, actualMead);
    }

    [Theory]
    [InlineData(3, 5)]
    [InlineData(1, 10)]
    public void Execute_ExcceedGasLimit_Throw(
        long gasLimit, long gasOwned)
    {
        if (gasLimit >= GasConsumption)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gasLimit),
                $"{nameof(gasLimit)} must be less than {GasConsumption}.");
        }

        if (gasOwned < GasConsumption)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gasOwned),
                $"{nameof(gasOwned)} must be greater than or equal to {GasConsumption}.");
        }

        // Given
        var amount = NCG * 1;
        var signerKey = new PrivateKey();
        var signerMead = Mead * gasOwned;
        var recipientKey = new PrivateKey();
        EnsureToMintAsset(signerKey, signerMead);
        EnsureToMintAsset(signerKey, NCG * 100);

        // When
        var expectedMead = signerMead - (Mead * gasLimit);
        var expectedNCG = NCG * 0;
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
        Assert.Single(innerExceptions);
        Assert.Contains(innerExceptions, i => i.Action is TransferAsset);
        Assert.Equal(expectedNCG, actualNCG);
        Assert.Equal(expectedMead, actualMead);
    }
}
