namespace Lib9c.Tests.Action.Factory
{
    using System.Collections.Generic;
    using System.Reflection;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume.Action.Factory;
    using Xunit;

    public class RapidCombinationFactoryTest
    {
        public static IEnumerable<object[]> Get_Create_By_ActionType_Success_MemberData()
        {
            const string prefix = "rapid_combination";
            for (var i = 0; i < 8; i++)
            {
                if (i == 0)
                {
                    yield return new object[] { prefix };
                    continue;
                }

                yield return new object[] { $"{prefix}{i + 1}" };
            }
        }

        [Theory]
        [InlineData(0L, int.MinValue)]
        [InlineData(long.MaxValue, int.MaxValue)]
        public void Create_By_BlockIndex_Success(long blockIndex, int slotIndex)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            var action = RapidCombinationFactory.Create(
                blockIndex,
                avatarAddr,
                slotIndex);
            Assert.Equal(avatarAddr, action.AvatarAddress);
            Assert.Equal(slotIndex, action.SlotIndex);
        }

        [Theory]
        [InlineData(long.MinValue)]
        [InlineData(-1L)]
        public void Create_By_BlockIndex_Throw_NotMatchFoundException(long blockIndex)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            Assert.Throws<NotMatchFoundException>(() =>
                RapidCombinationFactory.Create(
                    blockIndex,
                    avatarAddr,
                    0));
        }

        [Theory]
        [MemberData(nameof(Get_Create_By_ActionType_Success_MemberData))]
        public void Create_By_ActionType_Success(string actionType)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            var action = RapidCombinationFactory.Create(
                actionType,
                avatarAddr,
                0);
            var attr = action.GetType().GetCustomAttribute(typeof(ActionTypeAttribute))
                as ActionTypeAttribute;
            Assert.Equal(actionType, attr?.TypeIdentifier);
            Assert.Equal(avatarAddr, action.AvatarAddress);
            Assert.Equal(0, action.SlotIndex);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("hack_and_slash")]
        public void Create_By_ActionType_Throw_NotMatchFoundException(string actionType)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            Assert.Throws<NotMatchFoundException>(() =>
                RapidCombinationFactory.Create(
                    actionType,
                    avatarAddr,
                    0));
        }
    }
}
