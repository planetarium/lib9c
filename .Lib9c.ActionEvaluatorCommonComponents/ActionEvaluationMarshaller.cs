using System.Runtime.Serialization;
using System.Security.Cryptography;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Common;

namespace Lib9c.ActionEvaluatorCommonComponents
{
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
                dictionary["exception"] is Text typeName ? ResolveException(typeName) : null
            );
        }

        // Reconstruct an Exception whose runtime type matches the original FullName
        // recorded by Marshal, so that consumers calling GetType().FullName (e.g.
        // Libplanet's TxExecution) see the real action exception name rather than
        // a generic "System.Exception".
        private static Exception ResolveException(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, throwOnError: false);
                if (type is not null && typeof(Exception).IsAssignableFrom(type))
                {
                    try
                    {
                        return (Exception)FormatterServices.GetUninitializedObject(type);
                    }
                    catch
                    {
                        // fall through to generic fallback
                    }
                }
            }

            return new Exception(fullName);
        }

        public static ICommittedActionEvaluation Deserialize(byte[] serialized)
        {
            var decoded = Codec.Decode(serialized);
            return Unmarshal(decoded);
        }
    }
}
