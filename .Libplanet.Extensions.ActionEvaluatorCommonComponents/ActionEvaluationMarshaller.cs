using System.Reflection;
using System.Security.Cryptography;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Common;

namespace Libplanet.Extensions.ActionEvaluatorCommonComponents;

public static class ActionEvaluationMarshaller
{
    private static readonly Codec Codec = new Codec();

    public static byte[] Serialize(this ICommittedActionEvaluation actionEvaluation)
    {
        return Codec.Encode(Marshal(actionEvaluation));
    }

    public static Dictionary Marshal(this ICommittedActionEvaluation actionEvaluation) =>
        Dictionary.Empty
            .Add("action", actionEvaluation.Action)
            .Add("output_states", actionEvaluation.OutputState.ByteArray)
            .Add("input_context", actionEvaluation.InputContext.Marshal())
            .Add("exception", actionEvaluation.Exception?.GetType().FullName is { } typeName ? (Text)typeName : Null.Value);

    public static ICommittedActionEvaluation Unmarshal(IValue value)
    {
        if (value is not Dictionary dictionary)
        {
            throw new ArgumentException(nameof(value));
        }

        return new CommittedActionEvaluation(
            dictionary["action"],
            ActionContextMarshaller.Unmarshal((Dictionary)dictionary["input_context"]),
            new HashDigest<SHA256>(dictionary["output_states"]),
            GetException(dictionary)
        );
    }

    public static ICommittedActionEvaluation Deserialize(byte[] serialized)
    {
        var decoded = Codec.Decode(serialized);
        return Unmarshal(decoded);
    }

    private static Exception? GetException(Dictionary dictionary)
    {
        if (dictionary["exception"] is not Text typeName)
        {
            return null;
        }

        Type? type = Assembly.GetCallingAssembly().GetType(typeName.Value);
        if (type is null)
        {
            throw new ArgumentException($"Type {typeName.Value} not found in the calling assembly.");
        }

        object? instance = Activator.CreateInstance(type);

        if (instance is null)
        {
            throw new ArgumentException($"Failed to create an instance of {type}.");
        }

        return instance as Exception;
    }
}
