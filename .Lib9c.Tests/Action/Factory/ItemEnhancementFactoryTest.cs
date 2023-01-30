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
    using Xunit;

    public class ItemEnhancementFactoryTest
    {
        public static IEnumerable<object[]>
            Get_Create_By_BlockIndex_Success_MemberData()
        {
            yield return new object[]
            {
                0L,
                Guid.NewGuid(),
                Array.Empty<Guid>(),
                new PrivateKey().ToAddress(),
                int.MinValue,
            };
            yield return new object[]
            {
                ItemEnhancement0.ObsoleteIndex,
                Guid.NewGuid(),
                new[]
                {
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                },
                new PrivateKey().ToAddress(),
                int.MaxValue,
            };
        }

        public static IEnumerable<object[]>
            Get_CreateV2_By_BlockIndex_Success_MemberData()
        {
            yield return new object[]
            {
                ItemEnhancement0.ObsoleteIndex + 1,
                Guid.NewGuid(),
                Guid.NewGuid(),
                new PrivateKey().ToAddress(),
                int.MinValue,
            };
            yield return new object[]
            {
                long.MaxValue,
                Guid.NewGuid(),
                Guid.NewGuid(),
                new PrivateKey().ToAddress(),
                int.MaxValue,
            };
        }

        public static IEnumerable<object[]>
            Get_Create_By_ActionTypeIdentifier_Success_MemberData()
        {
            const string prefix = "item_enhancement";
            yield return new object[]
            {
                prefix,
                Guid.NewGuid(),
                Array.Empty<Guid>(),
                new PrivateKey().ToAddress(),
                int.MinValue,
            };
            yield return new object[]
            {
                prefix,
                Guid.NewGuid(),
                new[]
                {
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                },
                new PrivateKey().ToAddress(),
                int.MaxValue,
            };
        }

        public static IEnumerable<object[]>
            Get_CreateV2_By_ActionTypeIdentifier_Success_MemberData()
        {
            for (var i = 1; i < 11; i++)
            {
                var prefix = "item_enhancement";
                if (i > 0)
                {
                    prefix = $"{prefix}{i + 1}";
                }

                yield return new object[]
                {
                    prefix,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new PrivateKey().ToAddress(),
                    int.MinValue,
                };
                yield return new object[]
                {
                    prefix,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new PrivateKey().ToAddress(),
                    int.MaxValue,
                };
            }
        }

        [Theory]
        [MemberData(nameof(Get_Create_By_BlockIndex_Success_MemberData))]
        public void Create_By_BlockIndex_Success(
            long blockIndex,
            Guid itemId,
            Guid[] materialIds,
            Address avatarAddress,
            int slotIndex)
        {
            var action = ItemEnhancementFactory.Create(
                blockIndex,
                itemId,
                materialIds,
                avatarAddress,
                slotIndex);
            Assert.Equal(itemId, action.ItemId);
            Assert.True(materialIds.SequenceEqual(action.MaterialIds));
            Assert.Equal(avatarAddress, action.AvatarAddress);
            Assert.Equal(slotIndex, action.SlotIndex);
        }

        [Theory]
        [MemberData(nameof(Get_CreateV2_By_BlockIndex_Success_MemberData))]
        public void CreateV2_By_BlockIndex_Success(
            long blockIndex,
            Guid itemId,
            Guid materialId,
            Address avatarAddress,
            int slotIndex)
        {
            var action = ItemEnhancementFactory.Create(
                blockIndex,
                itemId,
                materialId,
                avatarAddress,
                slotIndex);
            Assert.Equal(itemId, action.ItemId);
            Assert.Equal(materialId, action.MaterialId);
            Assert.Equal(avatarAddress, action.AvatarAddress);
            Assert.Equal(slotIndex, action.SlotIndex);
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
        [InlineData(ItemEnhancement0.ObsoleteIndex)]
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
        [MemberData(nameof(Get_Create_By_ActionTypeIdentifier_Success_MemberData))]
        public void Create_By_ActionTypeIdentifier_Success(
            string actionTypeIdentifier,
            Guid itemId,
            Guid[] materialIds,
            Address avatarAddress,
            int slotIndex)
        {
            var action = ItemEnhancementFactory.Create(
                actionTypeIdentifier,
                itemId,
                materialIds,
                avatarAddress,
                slotIndex);
            var attr = action.GetType().GetCustomAttribute(typeof(ActionTypeAttribute))
                as ActionTypeAttribute;
            Assert.Equal(actionTypeIdentifier, attr?.TypeIdentifier);
            Assert.Equal(itemId, action.ItemId);
            Assert.Equal(materialIds, action.MaterialIds);
            Assert.Equal(avatarAddress, action.AvatarAddress);
            Assert.Equal(slotIndex, action.SlotIndex);
        }

        [Theory]
        [MemberData(nameof(Get_CreateV2_By_ActionTypeIdentifier_Success_MemberData))]
        public void CreateV2_By_ActionTypeIdentifier_Success(
            string actionTypeIdentifier,
            Guid itemId,
            Guid materialId,
            Address avatarAddress,
            int slotIndex)
        {
            var action = ItemEnhancementFactory.Create(
                actionTypeIdentifier,
                itemId,
                materialId,
                avatarAddress,
                slotIndex);
            var attr = action.GetType().GetCustomAttribute(typeof(ActionTypeAttribute))
                as ActionTypeAttribute;
            Assert.Equal(actionTypeIdentifier, attr?.TypeIdentifier);
            Assert.Equal(itemId, action.ItemId);
            Assert.Equal(materialId, action.MaterialId);
            Assert.Equal(avatarAddress, action.AvatarAddress);
            Assert.Equal(slotIndex, action.SlotIndex);
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
    }
}
