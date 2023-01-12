using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Lib9c.Action;
using Xunit;

namespace Lib9c.Tests.Action
{
    public class MemoLengthOverflowExceptionTest
    {
        [Fact]
        public void Serialize()
        {
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            var exc = new MemoLengthOverflowException("message");

            formatter.Serialize(ms, exc);
            ms.Seek(0, SeekOrigin.Begin);
            var deserialized =
                (MemoLengthOverflowException)formatter.Deserialize(ms);

            Assert.Equal(exc.Message, deserialized.Message);
        }
    }
}
