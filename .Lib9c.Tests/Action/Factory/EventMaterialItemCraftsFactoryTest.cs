namespace Lib9c.Tests.Action.Factory
{
    using System.Collections.Generic;
    using System.Reflection;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume.Action.Factory;
    using Xunit;

    public class EventMaterialItemCraftsFactoryTest
    {
        public static IEnumerable<object[]> Get_Create_By_ActionType_Success_MemberData()
        {
            const string prefix = "event_material_item_crafts";
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
        [InlineData(0L, int.MinValue, int.MinValue, null)]
        [InlineData(long.MaxValue, int.MaxValue, int.MaxValue, null)]
        public void Create_By_BlockIndex_Success(
            long blockIndex,
            int eventScheduleId,
            int eventMaterialItemRecipeId,
            Dictionary<int, int> materialsToUse)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            var action = EventMaterialItemCraftsFactory.Create(
                blockIndex,
                avatarAddr,
                eventScheduleId,
                eventMaterialItemRecipeId,
                materialsToUse);
            Assert.Equal(avatarAddr, action.AvatarAddress);
            Assert.Equal(eventScheduleId, action.EventScheduleId);
            Assert.Equal(eventMaterialItemRecipeId, action.EventMaterialItemRecipeId);
            Assert.Equal(materialsToUse, action.MaterialsToUse);
        }

        [Theory]
        [InlineData(long.MinValue)]
        [InlineData(-1L)]
        public void Create_By_BlockIndex_Throw_NotMatchFoundException(long blockIndex)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            Assert.Throws<NotMatchFoundException>(() =>
                EventMaterialItemCraftsFactory.Create(
                    blockIndex,
                    avatarAddr,
                    0,
                    0,
                    null));
        }

        [Theory]
        [MemberData(nameof(Get_Create_By_ActionType_Success_MemberData))]
        public void Create_By_ActionType_Success(string actionType)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            var action = EventMaterialItemCraftsFactory.Create(
                actionType,
                avatarAddr,
                0,
                0,
                null);
            var attr = action.GetType().GetCustomAttribute(typeof(ActionTypeAttribute))
                as ActionTypeAttribute;
            Assert.Equal(actionType, attr?.TypeIdentifier);
            Assert.Equal(avatarAddr, action.AvatarAddress);
            Assert.Equal(0, action.EventScheduleId);
            Assert.Equal(0, action.EventMaterialItemRecipeId);
            Assert.Null(action.MaterialsToUse);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("hack_and_slash")]
        public void Create_By_ActionType_Throw_NotMatchFoundException(string actionType)
        {
            var avatarAddr = new PrivateKey().ToAddress();
            Assert.Throws<NotMatchFoundException>(() =>
                EventMaterialItemCraftsFactory.Create(
                    actionType,
                    avatarAddr,
                    0,
                    0,
                    null));
        }
    }
}
