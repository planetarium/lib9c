using Bencodex;
using Bencodex.Types;
using Libplanet.Action.State;

namespace Libplanet.Extensions.ActionEvaluatorCommonComponents;

public static class WorldMarshaller
{
    private static readonly Codec Codec = new Codec();

    public static byte[] Serialize(this IWorld value)
    {
        return Codec.Encode(Marshal(value));
    }

    public static IEnumerable<Dictionary> Marshal(IEnumerable<IWorld> worlds)
    {
        foreach (var world in worlds)
        {
            var bdict = Marshal(world);
            yield return bdict;
        }
    }

    public static Dictionary Marshal(IWorld world)
    {
        var accounts = new Dictionary(
            world.Delta.Accounts.Select(
                kv => new KeyValuePair<Binary, Dictionary>(
                    new Binary(kv.Key.ByteArray),
                    AccountMarshaller.Marshal(kv.Value))));
        return accounts;
    }

    public static World Unmarshal(IValue marshalled)
    {
        return new World(marshalled);
    }

    public static World Deserialize(byte[] serialized)
    {
        var decoded = Codec.Decode(serialized);
        return Unmarshal(decoded);
    }
}
