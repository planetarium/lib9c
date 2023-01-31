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

    public class EventConsumableItemCraftsFactoryTest
    {
        public static IEnumerable<object[]>
            Get_Create_By_BlockIndex_Success_MemberData()
        {
            yield return new object[]
            {
                0L, typeof(EventConsumableItemCrafts),
            };
            yield return new object[]
            {
                long.MaxValue, typeof(EventConsumableItemCrafts),
            };
        }

        public static IEnumerable<object[]>
            Get_Create_By_ActionTypeIdentifier_Success_MemberData()
        {
            const string prefix = "event_consumable_item_crafts";
            for (var i = 0; i < 1; i++)
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
            var random = new Random();
            var eventScheduleId = random.Next(2) == 0
                ? int.MinValue
                : int.MaxValue;
            int eventConsumableItemRecipeId = random.Next(2) == 0
                ? int.MinValue
                : int.MaxValue;
            int slotIndex = random.Next(2) == 0
                ? int.MinValue
                : int.MaxValue;
            var action = EventConsumableItemCraftsFactory.Create(
                blockIndex,
                avatarAddr,
                eventScheduleId,
                eventConsumableItemRecipeId,
                slotIndex);
            AssertEquals(
                avatarAddr,
                eventScheduleId,
                eventConsumableItemRecipeId,
                slotIndex,
                action);
            Assert.IsType(expectedType, action);
        }

        [Theory]
        [InlineData(long.MinValue)]
        [InlineData(-1L)]
        public void Create_By_BlockIndex_Throw_NotMatchFoundException(long blockIndex)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            Assert.Throws<NotMatchFoundException>(() =>
                EventConsumableItemCraftsFactory.Create(
                    blockIndex,
                    avatarAddr,
                    0,
                    0,
                    0));
        }

        [Theory]
        [MemberData(nameof(Get_Create_By_ActionTypeIdentifier_Success_MemberData))]
        public void Create_By_ActionTypeIdentifier_Success(string actionTypeIdentifier)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            var random = new Random();
            var eventScheduleId = random.Next(2) == 0
                ? int.MinValue
                : int.MaxValue;
            int eventConsumableItemRecipeId = random.Next(2) == 0
                ? int.MinValue
                : int.MaxValue;
            int slotIndex = random.Next(2) == 0
                ? int.MinValue
                : int.MaxValue;
            var action = EventConsumableItemCraftsFactory.Create(
                actionTypeIdentifier,
                avatarAddr,
                eventScheduleId,
                eventConsumableItemRecipeId,
                slotIndex);
            var attr = action.GetType().GetCustomAttribute(typeof(ActionTypeAttribute))
                as ActionTypeAttribute;
            Assert.Equal(actionTypeIdentifier, attr?.TypeIdentifier);
            AssertEquals(
                avatarAddr,
                eventScheduleId,
                eventConsumableItemRecipeId,
                slotIndex,
                action);
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
                EventConsumableItemCraftsFactory.Create(
                    actionTypeIdentifier,
                    avatarAddr,
                    0,
                    0,
                    0));
        }

        private static void AssertEquals(
            Address avatarAddr,
            int eventScheduleId,
            int eventConsumableItemRecipeId,
            int slotIndex,
            IEventConsumableItemCrafts action)
        {
            Assert.Equal(avatarAddr, action.AvatarAddress);
            Assert.Equal(eventScheduleId, action.EventScheduleId);
            Assert.Equal(
                eventConsumableItemRecipeId,
                action.EventConsumableItemRecipeId);
            Assert.Equal(slotIndex, action.SlotIndex);
        }
    }
}
