namespace Lib9c.Tests.Action.Factory
{
    using System.Reflection;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume.Action.Factory;
    using Xunit;

    public class EventConsumableItemCraftsFactoryTest
    {
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
        [InlineData("event_consumable_item_crafts", int.MinValue, int.MinValue, int.MinValue)]
        [InlineData("event_consumable_item_crafts", int.MaxValue, int.MaxValue, int.MaxValue)]
        public void Create_By_ActionType_Success(
            string actionType,
            int eventScheduleId,
            int eventConsumableItemRecipeId,
            int slotIndex)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            var action = EventConsumableItemCraftsFactory.Create(
                actionType,
                avatarAddr,
                eventScheduleId,
                eventConsumableItemRecipeId,
                slotIndex);
            var attr = action.GetType().GetCustomAttribute(typeof(ActionTypeAttribute))
                as ActionTypeAttribute;
            Assert.Equal(actionType, attr?.TypeIdentifier);
            Assert.Equal(avatarAddr, action.AvatarAddress);
            Assert.Equal(eventScheduleId, action.EventScheduleId);
            Assert.Equal(eventConsumableItemRecipeId, action.EventConsumableItemRecipeId);
            Assert.Equal(slotIndex, action.SlotIndex);
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
