using System;
using System.Buffers;
using Libplanet;
using MessagePack;
using MessagePack.Formatters;

namespace Lib9c.Model.Order
{
    public class AddressFormatter : IMessagePackFormatter<Address>
    {
        public void Serialize(ref MessagePackWriter writer, Address value,
            MessagePackSerializerOptions options)
        {
            // if (value.Equals(default(Address)))
            // {
            //     writer.WriteNil();
            //     return;
            // }
            //
            writer.Write(value.ToByteArray());
        }

        Address IMessagePackFormatter<Address>.Deserialize(ref MessagePackReader reader,
            MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default;
            }

            options.Security.DepthStep(ref reader);

            return new Address(reader.ReadBytes()?.ToArray() ?? throw new InvalidOperationException());
        }
    }
}
