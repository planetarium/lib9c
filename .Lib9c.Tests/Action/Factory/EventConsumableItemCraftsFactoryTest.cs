namespace Lib9c.Tests.Action.Factory
{
    using System.Collections.Generic;
    using System.Reflection;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume.Action.Factory;
    using Xunit;

    public class EventConsumableItemCraftsFactoryTest
    {
        public static IEnumerable<object[]> Get_Create_By_ActionType_Success_MemberData()
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
        [InlineData(0L, int.MinValue, int.MinValue, int.MinValue)]
        [InlineData(long.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue)]
        public void Create_By_BlockIndex_Success(
            long blockIndex,
            int eventScheduleId,
            int eventConsumableItemRecipeId,
            int slotIndex)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            var action = EventConsumableItemCraftsFactory.Create(
                blockIndex,
                avatarAddr,
                eventScheduleId,
                eventConsumableItemRecipeId,
                slotIndex);
            Assert.Equal(avatarAddr, action.AvatarAddress);
            Assert.Equal(eventScheduleId, action.EventScheduleId);
            Assert.Equal(eventConsumableItemRecipeId, action.EventConsumableItemRecipeId);
            Assert.Equal(slotIndex, action.SlotIndex);
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
        [MemberData(nameof(Get_Create_By_ActionType_Success_MemberData))]
        public void Create_By_ActionType_Success(string actionType)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            var action = EventConsumableItemCraftsFactory.Create(
                actionType,
                avatarAddr,
                0,
                0,
                0);
            var attr = action.GetType().GetCustomAttribute(typeof(ActionTypeAttribute))
                as ActionTypeAttribute;
            Assert.Equal(actionType, attr?.TypeIdentifier);
            Assert.Equal(avatarAddr, action.AvatarAddress);
            Assert.Equal(0, action.EventScheduleId);
            Assert.Equal(0, action.EventConsumableItemRecipeId);
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
                EventConsumableItemCraftsFactory.Create(
                    actionType,
                    avatarAddr,
                    0,
                    0,
                    0));
        }
    }
}
