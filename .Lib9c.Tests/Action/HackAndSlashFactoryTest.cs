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
        [InlineData(18, null)]
        [InlineData(19, 1)]
        [InlineData(20, 1)]
        public void HackAndSlash(int version, int? stageBuffId)
        {
            var costumes = new List<Guid>();
            var equipments = new List<Guid>();
            var foods = new List<Guid>();
            var runes = new List<int>();
            var avatarAddress = new PrivateKey().ToAddress();
            var action = HackAndSlashFactory.HackAndSlash(
                version,
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
            IHackAndSlash action2 = null;
            if (version == 18)
            {
                action2 = new HackAndSlash18();
            }

            if (version == 19)
            {
                action2 = new HackAndSlash();
            }

            if (version == 20)
            {
                action2 = new HackAndSlash20();
            }

            var ga = (GameAction)action2;
            ga!.LoadPlainValue(action.PlainValue);
            Assert.Equal(action.PlainValue, ga.PlainValue);
        }
    }
}
