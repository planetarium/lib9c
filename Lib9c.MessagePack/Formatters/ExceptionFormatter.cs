using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bencodex.Types;
using MessagePack;
using MessagePack.Formatters;

namespace Lib9c.Formatters
{
    // FIXME: This class must be removed and replaced with other way for serialization.
    // https://github.com/dotnet/designs/blob/main/accepted/2020/better-obsoletion/binaryformatter-obsoletion.md
    public class ExceptionFormatter<T> : IMessagePackFormatter<T?> where T : Exception
    {
        public void Serialize(ref MessagePackWriter writer, T? value,
            MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            string typeName = value.GetType().AssemblyQualifiedName!;
            var msg = value.Message;
            var dict = new Dictionary<string, string>
            {
                ["type"] = typeName,
                ["msg"] = msg,
            };
            var bytes =  JsonSerializer.Serialize(dict);
            writer.Write(bytes);
        }

        public T? Deserialize(ref MessagePackReader reader,
            MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var a = reader.ReadString();
            var des = JsonSerializer.Deserialize<Dictionary<string, string>>(a);
            var exc = Activator.CreateInstance(Type.GetType(des!["type"])!, des["msg"])!;
            return (T)exc;
        }
    }
}
