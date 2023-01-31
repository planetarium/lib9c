namespace Lib9c.Tests.Action.Factory
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Action.Factory;
    using Nekoyume.Action.Interface;
    using Xunit;

    public class RapidCombinationFactoryTest
    {
        public static IEnumerable<object[]>
            Get_Create_By_BlockIndex_Success_MemberData()
        {
            yield return new object[]
            {
                0L, typeof(RapidCombination4),
            };
            yield return new object[]
            {
                RapidCombination0.ObsoleteIndex, typeof(RapidCombination4),
            };
            yield return new object[]
            {
                RapidCombination2.ObsoleteIndex, typeof(RapidCombination4),
            };
            yield return new object[]
            {
                RapidCombination3.ObsoleteIndex, typeof(RapidCombination4),
            };
            yield return new object[]
            {
                RapidCombination4.ObsoleteIndex, typeof(RapidCombination4),
            };
            yield return new object[]
            {
                RapidCombination4.ObsoleteIndex + 1, typeof(RapidCombination5),
            };
            yield return new object[]
            {
                RapidCombination5.ObsoleteIndex, typeof(RapidCombination5),
            };
            yield return new object[]
            {
                RapidCombination5.ObsoleteIndex + 1, typeof(RapidCombination7),
            };
            yield return new object[]
            {
                RapidCombination6.ObsoleteIndex, typeof(RapidCombination7),
            };
            yield return new object[]
            {
                RapidCombination7.ObsoleteIndex, typeof(RapidCombination7),
            };
            yield return new object[]
            {
                RapidCombination7.ObsoleteIndex + 1, typeof(RapidCombination),
            };
            yield return new object[]
            {
                long.MaxValue, typeof(RapidCombination),
            };
        }

        public static IEnumerable<object[]>
            Get_Create_By_ActionTypeIdentifier_Success_MemberData()
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
        [MemberData(nameof(Get_Create_By_BlockIndex_Success_MemberData))]
        public void Create_By_BlockIndex_Success(
            long blockIndex,
            Type expectedType)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            var slotIndex = new Random().Next(2) == 0
                ? int.MinValue
                : int.MaxValue;
            var action = RapidCombinationFactory.Create(
                blockIndex,
                avatarAddr,
                slotIndex);
            AssertEquals(avatarAddr, slotIndex, action);
            Assert.IsType(expectedType, action);
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
        [MemberData(nameof(Get_Create_By_ActionTypeIdentifier_Success_MemberData))]
        public void Create_By_ActionTypeIdentifier_Success(
            string actionTypeIdentifier)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            var slotIndex = new Random().Next(2) == 0
                ? int.MinValue
                : int.MaxValue;
            var action = RapidCombinationFactory.Create(
                actionTypeIdentifier,
                avatarAddr,
                slotIndex);
            var attr = action.GetType().GetCustomAttribute(typeof(ActionTypeAttribute))
                as ActionTypeAttribute;
            Assert.Equal(actionTypeIdentifier, attr?.TypeIdentifier);
            AssertEquals(avatarAddr, slotIndex, action);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("hack_and_slash")]
        public void Create_By_ActionTypeIdentifier_Throw_NotMatchFoundException(
            string actionTypeIdentifier)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            Assert.Throws<NotMatchFoundException>(() =>
                RapidCombinationFactory.Create(
                    actionTypeIdentifier,
                    avatarAddr,
                    0));
        }

        private static void AssertEquals(
            Address avatarAddr,
            int slotIndex,
            IRapidCombination action)
        {
            Assert.Equal(avatarAddr, action.AvatarAddress);
            Assert.Equal(slotIndex, action.SlotIndex);
        }
    }
}
