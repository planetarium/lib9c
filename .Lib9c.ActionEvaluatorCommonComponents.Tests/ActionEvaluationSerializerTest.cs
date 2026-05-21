using System.Collections.Immutable;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Common;
using Libplanet.Crypto;

namespace Lib9c.ActionEvaluatorCommonComponents.Tests;

public class ActionEvaluationSerializerTest
{
    [Fact]
    public void Serialization()
    {
        var addresses = Enumerable.Repeat(0, 4).Select(_ => new PrivateKey().Address).ToImmutableList();

        var random = new System.Random();
        var buffer = new byte[HashDigest<SHA256>.Size];
        random.NextBytes(buffer);
        var prevState = new HashDigest<SHA256>(buffer);
        random.NextBytes(buffer);
        var outputState = new HashDigest<SHA256>(buffer);
        random.NextBytes(buffer);
        var preEvalHash = new HashDigest<SHA256>(buffer);

        var committed = new CommittedActionEvaluation(
            action: Null.Value,
            inputContext: new CommittedActionContext(
                signer: addresses[0],
                txId: null,
                miner: addresses[1],
                blockIndex: 456,
                blockProtocolVersion: 0,
                previousState: prevState,
                randomSeed: 123,
                isPolicyAction: true),
            outputState: outputState,
            exception: new UnexpectedlyTerminatedActionException(
                "",
                preEvalHash,
                456,
                null,
                null,
                new NullAction(),
                new Exception()));
        var serialized = ActionEvaluationMarshaller.Serialize(committed);
        var deserialized = ActionEvaluationMarshaller.Deserialize(serialized);

        Assert.Equal(Null.Value, deserialized.Action);
        Assert.Equal(123, deserialized.InputContext.RandomSeed);
        Assert.Equal(456, deserialized.InputContext.BlockIndex);
        Assert.Equal(0, deserialized.InputContext.BlockProtocolVersion);
        Assert.Equal(addresses[0], deserialized.InputContext.Signer);
        Assert.Equal(addresses[1], deserialized.InputContext.Miner);
        Assert.Equal(prevState, deserialized.InputContext.PreviousState);
        Assert.Equal(outputState, deserialized.OutputState);
    }

    /// <summary>
    /// Verifies that the runtime type of an <see cref="Exception"/> attached to a
    /// <see cref="CommittedActionEvaluation"/> survives a marshal/unmarshal round trip,
    /// so that consumers reading <c>GetType().FullName</c> recover the original type.
    /// </summary>
    /// <param name="exceptionType">The exception type to instantiate and round trip.</param>
    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(ArgumentException))]
    [InlineData(typeof(UnexpectedlyTerminatedActionException))]
    public void ExceptionTypeNamePreservedAfterRoundTrip(Type exceptionType)
    {
        var address = new PrivateKey().Address;
        var buffer = new byte[HashDigest<SHA256>.Size];
        new System.Random().NextBytes(buffer);
        var stateHash = new HashDigest<SHA256>(buffer);

        Exception sourceException = exceptionType == typeof(UnexpectedlyTerminatedActionException)
            ? new UnexpectedlyTerminatedActionException("", stateHash, 0, null, null, new NullAction(), new Exception())
            : (Exception)Activator.CreateInstance(exceptionType)!;

        var committed = new CommittedActionEvaluation(
            action: Null.Value,
            inputContext: new CommittedActionContext(
                signer: address,
                txId: null,
                miner: address,
                blockIndex: 0,
                blockProtocolVersion: 0,
                previousState: stateHash,
                randomSeed: 0,
                isPolicyAction: false),
            outputState: stateHash,
            exception: sourceException);

        var serialized = ActionEvaluationMarshaller.Serialize(committed);
        var deserialized = ActionEvaluationMarshaller.Deserialize(serialized);

        Assert.NotNull(deserialized.Exception);
        Assert.Equal(exceptionType.FullName, deserialized.Exception!.GetType().FullName);
    }

    /// <summary>
    /// Verifies that the full <see cref="Exception.InnerException"/> chain round
    /// trips through the marshaller. Mirrors the real PAEV path where Libplanet
    /// wraps the action exception in <see cref="UnexpectedlyTerminatedActionException"/>
    /// and later unwraps one level to record the original cause's type name.
    /// </summary>
    [Fact]
    public void InnerExceptionChainPreservedAfterRoundTrip()
    {
        // Mirrors the real PAEV path: Libplanet wraps the action exception in
        // UnexpectedlyTerminatedActionException whose InnerException is the
        // original cause. Both must round-trip so that Libplanet's TxExecution
        // unwrap step can surface the inner type name.
        var address = new PrivateKey().Address;
        var buffer = new byte[HashDigest<SHA256>.Size];
        new System.Random().NextBytes(buffer);
        var stateHash = new HashDigest<SHA256>(buffer);

        var inner = new InvalidOperationException("inner cause");
        var outer = new UnexpectedlyTerminatedActionException(
            "wrapped",
            stateHash,
            0,
            null,
            null,
            new NullAction(),
            inner);

        var committed = new CommittedActionEvaluation(
            action: Null.Value,
            inputContext: new CommittedActionContext(
                signer: address,
                txId: null,
                miner: address,
                blockIndex: 0,
                blockProtocolVersion: 0,
                previousState: stateHash,
                randomSeed: 0,
                isPolicyAction: false),
            outputState: stateHash,
            exception: outer);

        var serialized = ActionEvaluationMarshaller.Serialize(committed);
        var deserialized = ActionEvaluationMarshaller.Deserialize(serialized);

        Assert.NotNull(deserialized.Exception);
        Assert.Equal(
            typeof(UnexpectedlyTerminatedActionException).FullName,
            deserialized.Exception!.GetType().FullName);
        Assert.NotNull(deserialized.Exception.InnerException);
        Assert.Equal(
            typeof(InvalidOperationException).FullName,
            deserialized.Exception.InnerException!.GetType().FullName);
    }

    /// <summary>
    /// Verifies backward compatibility with the legacy on-the-wire format that
    /// stored the exception as a single Bencodex <c>Text</c> type name, ensuring a
    /// new headless build can still parse messages produced by an older plugin DLL.
    /// </summary>
    [Fact]
    public void UnmarshalBackwardCompatibleWithLegacyTextFormat()
    {
        // The previous on-the-wire format stored a single Text typeName for
        // the exception field. Make sure new headless can still parse messages
        // produced by an older plugin DLL.
        var buffer = new byte[HashDigest<SHA256>.Size];
        new System.Random().NextBytes(buffer);
        var stateHash = new HashDigest<SHA256>(buffer);
        var address = new PrivateKey().Address;

        var legacy = Bencodex.Types.Dictionary.Empty
            .Add("action", Null.Value)
            .Add("output_states", stateHash.ByteArray)
            .Add(
                "input_context",
                new CommittedActionContext(
                    signer: address,
                    txId: null,
                    miner: address,
                    blockIndex: 0,
                    blockProtocolVersion: 0,
                    previousState: stateHash,
                    randomSeed: 0,
                    isPolicyAction: false).Marshal())
            .Add("exception", (Text)typeof(InvalidOperationException).FullName!);

        var deserialized = ActionEvaluationMarshaller.Unmarshal(legacy);

        Assert.NotNull(deserialized.Exception);
        Assert.Equal(
            typeof(InvalidOperationException).FullName,
            deserialized.Exception!.GetType().FullName);
    }
}
