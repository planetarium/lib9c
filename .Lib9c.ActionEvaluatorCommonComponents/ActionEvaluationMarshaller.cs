using System.Reflection;
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

        // Libplanet's TxExecution unwraps one level of InnerException before
        // recording the type name, so without serializing the chain a wrapping
        // exception (e.g. UnexpectedlyTerminatedActionException) hides the real
        // action exception.
        private static readonly FieldInfo InnerExceptionField =
            typeof(Exception).GetField(
                "_innerException",
                BindingFlags.Instance | BindingFlags.NonPublic)!;

        public static byte[] Serialize(this ICommittedActionEvaluation actionEvaluation)
        {
            return Codec.Encode(Marshal(actionEvaluation));
        }

        public static Dictionary Marshal(this ICommittedActionEvaluation actionEvaluation) =>
            Dictionary.Empty
                .Add("action", actionEvaluation.Action)
                .Add("output_states", actionEvaluation.OutputState.ByteArray)
                .Add("input_context", actionEvaluation.InputContext.Marshal())
                .Add("exception", MarshalException(actionEvaluation.Exception));

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
                UnmarshalException(dictionary["exception"])
            );
        }

        // Serialize the InnerException chain (outer -> inner) so that consumers
        // can recover both the wrapping exception type and the original cause.
        private static IValue MarshalException(Exception? exception)
        {
            if (exception is null)
            {
                return Null.Value;
            }

            var chain = new System.Collections.Generic.List<IValue>();
            for (var current = exception; current is not null; current = current.InnerException)
            {
                var fullName = current.GetType().FullName ?? typeof(Exception).FullName!;
                chain.Add((Text)fullName);
            }

            return new Bencodex.Types.List(chain);
        }

        private static Exception? UnmarshalException(IValue value)
        {
            switch (value)
            {
                case Null:
                    return null;
                case Text singleTypeName:
                    // Backward compatibility with the previous format that
                    // stored only the outer exception's type name.
                    return ResolveException(singleTypeName);
                case Bencodex.Types.List list when list.Count > 0:
                    Exception? inner = null;
                    for (var i = list.Count - 1; i >= 0; i--)
                    {
                        if (list[i] is not Text typeName)
                        {
                            continue;
                        }

                        var outer = ResolveException(typeName);
                        if (inner is not null)
                        {
                            InnerExceptionField.SetValue(outer, inner);
                        }

                        inner = outer;
                    }

                    return inner;
                default:
                    return null;
            }
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
