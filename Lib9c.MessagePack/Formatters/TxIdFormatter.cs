using System;
using System.Buffers;
using System.Collections.Immutable;
using Libplanet.Types.Tx;
using MessagePack;
using MessagePack.Formatters;

namespace Lib9c.Formatters
{
    public class TxIdFormatter : IMessagePackFormatter<TxId?>
    {
        public void Serialize(ref MessagePackWriter writer, TxId? value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            writer.Write(value.Value.ToByteArray());
        }

        public TxId? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }

            options.Security.DepthStep(ref reader);

            var bytes = reader.ReadBytes()?.ToArray();
            if (bytes is null)
            {
                throw new InvalidOperationException();
            }

            return new TxId(bytes.ToImmutableArray());
        }
    }
}
