namespace Lib9c.Tests.Module
{
    using System;
    using System.Linq;
    using Libplanet.Common;
    using Nekoyume.Module;
    using Xunit;

    public class InfiniteTowerModuleAddressTest
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(15)]
        [InlineData(26)]
        [InlineData(255)]
        [InlineData(int.MaxValue)]
        [InlineData(-1)]
        public void GetBoardStateAddress_Does_Not_Throw_For_Any_Id(int id)
        {
            var address = InfiniteTowerModule.GetBoardStateAddress(id);

            // Same bytes as the former upper-case X40 derivation, so existing states stay reachable.
            Assert.Equal(
                ByteUtil.ParseHex($"{id:X40}"),
                address.ByteArray.ToArray());
        }

        [Fact]
        public void GetBoardStateAddress_Is_Unique_Per_Id()
        {
            var addresses = Enumerable.Range(0, 1000)
                .Select(InfiniteTowerModule.GetBoardStateAddress)
                .ToList();

            Assert.Equal(addresses.Count, addresses.Distinct().Count());
        }
    }
}
