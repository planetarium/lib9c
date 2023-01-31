namespace Lib9c.Tests.Action.Factory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Action.Factory;
    using Nekoyume.Action.Interface;
    using Xunit;

    public class ItemEnhancementFactoryTest
    {
        public static IEnumerable<object[]>
            Get_Create_By_BlockIndex_Success_MemberData()
        {
            yield return new object[]
            {
                0L, typeof(ItemEnhancement0),
            };
            yield return new object[]
            {
                ItemEnhancement0.ObsoleteIndex, typeof(ItemEnhancement0),
            };
        }

        public static IEnumerable<object[]>
            Get_CreateV2_By_BlockIndex_Success_MemberData()
        {
            yield return new object[]
            {
                0L, typeof(ItemEnhancement8),
            };
            yield return new object[]
            {
                ItemEnhancement8.ObsoleteIndex, typeof(ItemEnhancement8),
            };
            yield return new object[]
            {
                ItemEnhancement8.ObsoleteIndex + 1, typeof(ItemEnhancement9),
            };
            yield return new object[]
            {
                ItemEnhancement9.ObsoleteIndex, typeof(ItemEnhancement9),
            };
            yield return new object[]
            {
                ItemEnhancement9.ObsoleteIndex + 1, typeof(ItemEnhancement),
            };
            yield return new object[]
            {
                long.MaxValue, typeof(ItemEnhancement),
            };
        }

        public static IEnumerable<object[]>
            Get_CreateV2_By_ActionTypeIdentifier_Success_MemberData()
        {
            for (var i = 1; i < 11; i++)
            {
                yield return new object[] { $"item_enhancement{i + 1}" };
            }
        }

        [Theory]
        [MemberData(nameof(Get_Create_By_BlockIndex_Success_MemberData))]
        public void Create_By_BlockIndex_Success(
            long blockIndex,
            Type expectedType)
        {
            var itemId = Guid.NewGuid();
            Guid[] materialIds = new Random().Next(2) == 0
                ? Array.Empty<Guid>()
                : new[] { Guid.NewGuid(), Guid.NewGuid(), };
            var avatarAddress = new PrivateKey().ToAddress();
            var slotIndex = new Random().Next(2) == 0
                ? int.MinValue
                : int.MaxValue;
            var action = ItemEnhancementFactory.Create(
                blockIndex,
                itemId,
                materialIds,
                avatarAddress,
                slotIndex);
            AssertEquals(itemId, materialIds, avatarAddress, slotIndex, action);
            Assert.IsType(expectedType, action);
        }

        [Theory]
        [MemberData(nameof(Get_CreateV2_By_BlockIndex_Success_MemberData))]
        public void CreateV2_By_BlockIndex_Success(
            long blockIndex,
            Type expectedType)
        {
            var itemId = Guid.NewGuid();
            var materialId = Guid.NewGuid();
            var avatarAddress = new PrivateKey().ToAddress();
            var slotIndex = new Random().Next(2) == 0
                ? int.MinValue
                : int.MaxValue;
            var action = ItemEnhancementFactory.Create(
                blockIndex,
                itemId,
                materialId,
                avatarAddress,
                slotIndex);
            AssertEquals(itemId, materialId, avatarAddress, slotIndex, action);
            Assert.IsType(expectedType, action);
        }

        [Theory]
        [InlineData(long.MinValue)]
        [InlineData(-1L)]
        [InlineData(ItemEnhancement0.ObsoleteIndex + 1)]
        public void Create_By_BlockIndex_Throw_NotMatchFoundException(long blockIndex)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            Assert.Throws<NotMatchFoundException>(() =>
                ItemEnhancementFactory.Create(
                    blockIndex,
                    Guid.NewGuid(),
                    Array.Empty<Guid>(),
                    avatarAddr,
                    0));
        }

        [Theory]
        [InlineData(long.MinValue)]
        [InlineData(-1L)]
        public void CreateV2_By_BlockIndex_Throw_NotMatchFoundException(long blockIndex)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            Assert.Throws<NotMatchFoundException>(() =>
                ItemEnhancementFactory.Create(
                    blockIndex,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    avatarAddr,
                    0));
        }

        [Theory]
        [InlineData("item_enhancement")]
        public void Create_By_ActionTypeIdentifier_Success(string actionTypeIdentifier)
        {
            var itemId = Guid.NewGuid();
            Guid[] materialIds = new Random().Next(2) == 0
                ? Array.Empty<Guid>()
                : new[] { Guid.NewGuid(), Guid.NewGuid(), };
            var avatarAddress = new PrivateKey().ToAddress();
            var slotIndex = new Random().Next(2) == 0
                ? int.MinValue
                : int.MaxValue;
            var action = ItemEnhancementFactory.Create(
                actionTypeIdentifier,
                itemId,
                materialIds,
                avatarAddress,
                slotIndex);
            var attr = action.GetType().GetCustomAttribute(typeof(ActionTypeAttribute))
                as ActionTypeAttribute;
            Assert.Equal(actionTypeIdentifier, attr?.TypeIdentifier);
            AssertEquals(itemId, materialIds, avatarAddress, slotIndex, action);
        }

        [Theory]
        [MemberData(nameof(Get_CreateV2_By_ActionTypeIdentifier_Success_MemberData))]
        public void CreateV2_By_ActionTypeIdentifier_Success(
            string actionTypeIdentifier)
        {
            var itemId = Guid.NewGuid();
            var materialId = Guid.NewGuid();
            var avatarAddress = new PrivateKey().ToAddress();
            var slotIndex = new Random().Next(2) == 0
                ? int.MinValue
                : int.MaxValue;
            var action = ItemEnhancementFactory.Create(
                actionTypeIdentifier,
                itemId,
                materialId,
                avatarAddress,
                slotIndex);
            var attr = action.GetType().GetCustomAttribute(typeof(ActionTypeAttribute))
                as ActionTypeAttribute;
            Assert.Equal(actionTypeIdentifier, attr?.TypeIdentifier);
            AssertEquals(itemId, materialId, avatarAddress, slotIndex, action);
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
                ItemEnhancementFactory.Create(
                    actionTypeIdentifier,
                    Guid.NewGuid(),
                    Array.Empty<Guid>(),
                    avatarAddr,
                    0));
            Assert.Throws<NotMatchFoundException>(() =>
                ItemEnhancementFactory.Create(
                    actionTypeIdentifier,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    avatarAddr,
                    0));
        }

        private static void AssertEquals(
            Guid itemId,
            Guid[] materialIds,
            Address avatarAddr,
            int slotIndex,
            IItemEnhancement action)
        {
            Assert.Equal(itemId, action.ItemId);
            Assert.True(materialIds.SequenceEqual(action.MaterialIds));
            Assert.Equal(avatarAddr, action.AvatarAddress);
            Assert.Equal(slotIndex, action.SlotIndex);
        }

        private static void AssertEquals(
            Guid itemId,
            Guid materialId,
            Address avatarAddr,
            int slotIndex,
            IItemEnhancementV2 action)
        {
            Assert.Equal(itemId, action.ItemId);
            Assert.Equal(materialId, action.MaterialId);
            Assert.Equal(avatarAddr, action.AvatarAddress);
            Assert.Equal(slotIndex, action.SlotIndex);
        }
    }
}
