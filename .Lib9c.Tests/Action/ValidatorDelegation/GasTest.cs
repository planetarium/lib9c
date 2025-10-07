#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation;

using System;
using System.Linq;
using Libplanet.Action;
using Libplanet.Crypto;
using Lib9c.Action;
using Xunit;

public class GasTest : TxAcitonTestBase
{
    [Theory]
    [InlineData(0, 0, 4)]
    [InlineData(1, 1, 4)]
    [InlineData(4, 4, 4)]
    public void Execute(long gasLimit, long gasConsumption, long gasOwned)
    {
        if (gasLimit < gasConsumption)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gasLimit),
                $"{nameof(gasLimit)} must be greater than or equal to {nameof(gasConsumption)}.");
        }

        if (gasOwned < gasConsumption)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gasOwned),
                $"{nameof(gasOwned)} must be greater than or equal to {nameof(gasConsumption)}.");
        }

        // Given
        var signerKey = new PrivateKey();
        var signerMead = Mead * gasOwned;
        EnsureToMintAsset(signerKey, signerMead);

        // When
        var expectedMead = signerMead - Mead * gasConsumption;
        var gasActions = new IAction[]
        {
            new GasAction { Consumption = gasConsumption },
        };

        MakeTransaction(
            signerKey,
            gasActions,
            maxGasPrice: Mead * 1,
            gasLimit: gasLimit);
        MoveToNextBlock(throwOnError: true);

        // Then
        var actualMead = GetBalance(signerKey.Address, Mead);
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
        EnsureToMintAsset(signerKey, signerMead);

        // When
        var expectedMead = signerMead;
        var gasActions = new IAction[]
        {
            new GasAction { Consumption = gasConsumption },
        };

        MakeTransaction(signerKey, gasActions);
        MoveToNextBlock(throwOnError: true);

        // Then
        var actualMead = GetBalance(signerKey.Address, Mead);
        Assert.Equal(expectedMead, actualMead);
    }

    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(4, 4, 0)]
    [InlineData(4, 4, 1)]
    public void Execute_InsufficientMead_Throw(
        long gasLimit, long gasConsumption, long gasOwned)
    {
        if (gasLimit < gasConsumption)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gasLimit),
                $"{nameof(gasLimit)} must be greater than or equal to {nameof(gasConsumption)}.");
        }

        if (gasOwned >= gasConsumption)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gasOwned),
                $"{nameof(gasOwned)} must be less than {nameof(gasConsumption)}.");
        }

        // Given
        var signerKey = new PrivateKey();
        var signerMead = Mead * gasOwned;
        EnsureToMintAsset(signerKey, signerMead);

        // When
        var expectedMead = Mead * 0;
        var gasAction = new GasAction { Consumption = gasConsumption };
        MakeTransaction(
            signerKey,
            new ActionBase[] { gasAction, },
            maxGasPrice: Mead * 1,
            gasLimit: gasLimit);

        // Then
        var e = Assert.Throws<AggregateException>(() => MoveToNextBlock(throwOnError: true));
        var innerExceptions = e.InnerExceptions
            .Cast<UnexpectedlyTerminatedActionException>()
            .ToArray();
        var actualMead = GetBalance(signerKey.Address, Mead);
        Assert.Single(innerExceptions);
        Assert.IsType<GasAction>(innerExceptions[0].Action);
        Assert.Equal(expectedMead, actualMead);
    }

    [Theory]
    [InlineData(4, 5, 5)]
    [InlineData(1, 5, 10)]
    public void Execute_ExcceedGasLimit_Throw(
        long gasLimit, long gasConsumption, long gasOwned)
    {
        if (gasLimit >= gasConsumption)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gasLimit),
                $"{nameof(gasLimit)} must be less than {nameof(gasConsumption)}.");
        }

        if (gasOwned < gasConsumption)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gasOwned),
                $"{nameof(gasOwned)} must be greater than or equal to {nameof(gasConsumption)}.");
        }

        // Given
        var signerKey = new PrivateKey();
        var signerMead = Mead * gasOwned;
        EnsureToMintAsset(signerKey, signerMead);

        // When
        var expectedMead = signerMead - (Mead * gasLimit);
        var gasAction = new GasAction { Consumption = gasConsumption };
        MakeTransaction(
            signerKey,
            new ActionBase[] { gasAction, },
            maxGasPrice: Mead * 1,
            gasLimit: gasLimit);

        // Then
        var e = Assert.Throws<AggregateException>(() => MoveToNextBlock(throwOnError: true));
        var innerExceptions = e.InnerExceptions
            .Cast<UnexpectedlyTerminatedActionException>()
            .ToArray();
        var actualMead = GetBalance(signerKey.Address, Mead);
        Assert.Single(innerExceptions);
        Assert.Contains(innerExceptions, i => i.Action is GasAction);
        Assert.Equal(expectedMead, actualMead);
    }
}
