namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Xunit;

    public class HackAndSlashFactoryTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        public void HackAndSlash(int? stageBuffId)
        {
            var costumes = new List<Guid>();
            var equipments = new List<Guid>();
            var foods = new List<Guid>();
            var runes = new List<int>();
            var avatarAddress = new PrivateKey().ToAddress();
            var action = HackAndSlashFactory.HackAndSlash(
                2L,
                costumes,
                equipments,
                foods,
                runes,
                1,
                2,
                avatarAddress,
                1,
                stageBuffId
            );
            Assert.IsType<HackAndSlash>(action);
            var action2 = new HackAndSlash();
            action2.LoadPlainValue(action.PlainValue);
            Assert.Equal(action.PlainValue, action2.PlainValue);
        }
    }
}
