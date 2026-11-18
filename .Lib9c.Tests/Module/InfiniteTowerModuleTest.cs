namespace Lib9c.Tests.Module
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class InfiniteTowerModuleTest
    {
        [Fact]
        public void UpperCaseHex_FailsChecksum_ForId11()
        {
            // The former derivation. Keeps the cause of PLD-1757 visible.
            Assert.Throws<ArgumentException>(() => new Address($"{11:X40}"));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(12)]
        [InlineData(13)]
        [InlineData(26)]
        public void BoardState_RoundTrips(int infiniteTowerId)
        {
            var boardState = new InfiniteTowerBoardState(infiniteTowerId);
            boardState.RecordFloorClear(1, 100L);

            IWorld world = new World(MockUtil.MockModernWorldState)
                .SetInfiniteTowerBoardState(boardState);

            var loaded = world.GetInfiniteTowerBoardState(infiniteTowerId);
            Assert.Equal(infiniteTowerId, loaded.InfiniteTowerId);
            Assert.Equal(1, loaded.GetFloorClearCount(1));
        }

        [Fact]
        public void GetBoardStateAddress_KeepsFormerBytes_AndIsUnique()
        {
            var ids = Enumerable.Range(0, 1000).Append(int.MaxValue).Append(-1).ToList();
            var addresses = new HashSet<Address>();
            foreach (var id in ids)
            {
                var address = InfiniteTowerModule.GetBoardStateAddress(id);

                // Same bytes as the former upper-case X40 derivation, so existing states stay reachable.
                Assert.Equal(ByteUtil.ParseHex($"{id:X40}"), address.ByteArray.ToArray());
                Assert.True(addresses.Add(address));
            }
        }
    }
}
