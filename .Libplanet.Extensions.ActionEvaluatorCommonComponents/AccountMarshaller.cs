using Bencodex;
using Bencodex.Types;
using Libplanet.Action.State;

namespace Libplanet.Extensions.ActionEvaluatorCommonComponents;

public static class AccountMarshaller
{
    private static readonly Codec Codec = new Codec();

    public static byte[] Serialize(this IAccount value)
    {
        return Codec.Encode(Marshal(value));
    }

    public static IEnumerable<Dictionary> Marshal(IEnumerable<IAccount> stateDeltas)
    {
        foreach (var stateDelta in stateDeltas)
        {
            var bdict = Marshal(stateDelta);
            yield return bdict;
        }
    }

    public static Dictionary Marshal(IAccount account)
    {
        var state = new Dictionary(account.Delta.States.Select(
            kv => new KeyValuePair<IKey, IValue>(
                new Binary(kv.Key.ByteArray),
                kv.Value)));
        var balance = new List(account.Delta.Fungibles.Select(
            kv => Dictionary.Empty
                .Add("address", new Binary(kv.Key.Item1.ByteArray))
                .Add("currency", kv.Key.Item2.Serialize())
                .Add("amount", new Integer(kv.Value))));
        var totalSupply = new List(account.Delta.TotalSupplies.Select(
            kv => Dictionary.Empty
                .Add("currency", kv.Key.Serialize())
                .Add("amount", new Integer(kv.Value))));
        var bdict = Dictionary.Empty
            .Add("states", state)
            .Add("balances", balance)
            .Add("totalSupplies", totalSupply)
            .Add("validatorSet", account.Delta.ValidatorSet is { } validatorSet
                ? validatorSet.Bencoded
                : Null.Value);
        return bdict;
    }

    public static Account Unmarshal(IValue marshalled)
    {
        return new Account(marshalled);
    }

    public static Account Deserialize(byte[] serialized)
    {
        var decoded = Codec.Decode(serialized);
        return Unmarshal(decoded);
    }
}
