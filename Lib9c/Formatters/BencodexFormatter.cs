using System;
using System.Buffers;
using Bencodex;
using Bencodex.Types;
using MessagePack;
using MessagePack.Formatters;

namespace Lib9c.Formatters
{
    public class BencodexFormatter : IMessagePackFormatter<IValue>
    {
        public void Serialize(ref MessagePackWriter writer, IValue value, MessagePackSerializerOptions options)
        {
            writer.Write(new Codec().Encode(value));
        }

        IValue IMessagePackFormatter<IValue>.Deserialize(ref MessagePackReader reader,
            MessagePackSerializerOptions options)
        {
            options.Security.DepthStep(ref reader);

            return new Codec().Decode(reader.ReadBytes()?.ToArray() ?? throw new InvalidOperationException());
        }

    }
}
