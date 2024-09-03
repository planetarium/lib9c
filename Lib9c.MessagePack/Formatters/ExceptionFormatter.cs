using System;
using System.Reflection;
using System.Runtime.Serialization;
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

            var info = new SerializationInfo(typeof(T), new FormatterConverter());
            value.GetObjectData(info, new StreamingContext(StreamingContextStates.All));

            writer.WriteMapHeader(info.MemberCount + 1);
            writer.Write("ExceptionType");
            writer.Write("System.Exception");

            foreach (SerializationEntry entry in info)
            {
                writer.Write(entry.Name);
                writer.Write(entry.ObjectType.FullName);
                MessagePackSerializer.Serialize(
                    entry.ObjectType,
                    ref writer,
                    entry.Name == "Message" ? value.Message : entry.Value,
                    options);
            }
        }

        public T? Deserialize(ref MessagePackReader reader,
            MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            var count = reader.ReadMapHeader();

            var info = new SerializationInfo(typeof(T), new FormatterConverter());
            string? typeName = null;

            for (int i = 0; i < count; i++)
            {
                var name = reader.ReadString();
                if (name == "ExceptionType")
                {
                    typeName = reader.ReadString();
                }
                else
                {
                    var type = Type.GetType(reader.ReadString());
                    var value = MessagePackSerializer.Deserialize(type, ref reader, options);
                    info.AddValue(name, value);
                }
            }

            if (typeName == null)
            {
                throw new MessagePackSerializationException("Exception type information is missing.");
            }

            var exceptionType = Type.GetType(typeName);
            if (exceptionType == null)
            {
                throw new MessagePackSerializationException($"Exception type '{typeName}' not found.");
            }

            var ctor = exceptionType.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                new Type[] { typeof(SerializationInfo), typeof(StreamingContext) },
                null
            );

            if (ctor != null)
            {
                return (T)ctor.Invoke(new object[] { info, new StreamingContext(StreamingContextStates.All) });
            }

            var message = info.GetString("Message");
            var innerException = (Exception?)info.GetValue("InnerException", typeof(Exception));

            T? exception;
            var constructorWithInnerException = exceptionType.GetConstructor(new[] { typeof(string), typeof(Exception) });
            if (constructorWithInnerException != null)
            {
                exception = (T)constructorWithInnerException.Invoke(new object[] { message!, innerException! });
            }
            else
            {
                var constructorWithMessage = exceptionType.GetConstructor(new[] { typeof(string) });
                if (constructorWithMessage != null)
                {
                    exception = (T)constructorWithMessage.Invoke(new object[] { message! });
                }
                else
                {
                    exception = (T?)Activator.CreateInstance(exceptionType);
                }
            }

            var stackTrace = info.GetString("StackTraceString");
            if (!string.IsNullOrEmpty(stackTrace))
            {
                var stackTraceField = typeof(Exception).GetField("_stackTraceString", BindingFlags.NonPublic | BindingFlags.Instance);
                stackTraceField?.SetValue(exception, stackTrace);
            }

            return exception;
        }
    }
}
