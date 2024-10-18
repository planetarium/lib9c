namespace Lib9c.Tests.Extensions
{
    using System;
    using Bencodex.Types;
    using Nekoyume.Exceptions;
    using Xunit;

    public class BencodexTypesExtensionsTest
    {
        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(1, 0, false)]
        [InlineData(2, 1, false)]
        [InlineData(2, 2, true)]
        public void Replace_List(int count, int index, bool throwException)
        {
            var list = new List(new int[count]);
            if (throwException)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    list.Replace(index, (Integer)1));
            }
            else
            {
                list = list.Replace(index, (Integer)1);
                Assert.Equal(1, (int)(Integer)list[index]);
            }
        }
    }
}
