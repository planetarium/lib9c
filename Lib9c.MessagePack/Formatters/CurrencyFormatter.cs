using System;
using System.Buffers;
using MessagePack;
using Libplanet.Types.Assets;
using MessagePack.Formatters;
using Bencodex;

namespace Lib9c.Formatters
{
    public class CurrencyFormatter : IMessagePackFormatter<Currency>
    {
        public void Serialize(ref MessagePackWriter writer, Currency value, MessagePackSerializerOptions options)
        {
            if (value.Equals(default))
            {
                writer.WriteNil();
                return;
            }
            
            writer.Write(new Codec().Encode(value.Serialize()));
        }

        public Currency Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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

            return new Currency(new Codec().Decode(bytes));
        }
    }
}
