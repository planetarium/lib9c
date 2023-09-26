using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action.State;
using MessagePack;
using MessagePack.Formatters;

namespace Lib9c.Formatters
{
    public class WorldFormatter : IMessagePackFormatter<IWorld>
    {
        public void Serialize(ref MessagePackWriter writer, IWorld value,
            MessagePackSerializerOptions options)
        {
            var accounts = new Dictionary(
                value.Delta.Accounts.Select(
                    account => new KeyValuePair<IKey, IValue>(
                        (Binary)account.Key.ToByteArray(),
                        new Account(
                            account.Value.Delta.States,
                            account.Value.Delta.Fungibles,
                            account.Value.Delta.TotalSupplies).Serialize()))
            );

            var bdict = new Dictionary(new[]
            {
                new KeyValuePair<IKey, IValue>((Text) "accounts", accounts)
            });

            writer.Write(new Codec().Encode(bdict));
        }

        public IWorld Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            options.Security.DepthStep(ref reader);

            var bytes = reader.ReadBytes();
            if (bytes is null)
            {
                throw new NullReferenceException($"ReadBytes from serialized {nameof(IAccount)} is null.");
            }

            return new World(new Codec().Decode(bytes.Value.ToArray()));
        }
    }
}
