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

    public class EventMaterialItemCraftsFactoryTest
    {
        public static IEnumerable<object[]>
            Get_Create_By_BlockIndex_Success_MemberData()
        {
            yield return new object[]
            {
                0L, typeof(EventMaterialItemCrafts),
            };
            yield return new object[]
            {
                long.MaxValue, typeof(EventMaterialItemCrafts),
            };
        }

        public static IEnumerable<object[]>
            Get_Create_By_ActionTypeIdentifier_Success_MemberData()
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
            var eventMaterialItemRecipeId = random.Next(2) == 0
                ? int.MinValue
                : int.MaxValue;
            Dictionary<int, int> materialsToUse = random.Next(2) == 0
                ? null
                : new Dictionary<int, int>();
            var action = EventMaterialItemCraftsFactory.Create(
                blockIndex,
                avatarAddr,
                eventScheduleId,
                eventMaterialItemRecipeId,
                materialsToUse);
            AssertEquals(
                avatarAddr,
                eventScheduleId,
                eventMaterialItemRecipeId,
                materialsToUse,
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
                EventMaterialItemCraftsFactory.Create(
                    blockIndex,
                    avatarAddr,
                    0,
                    0,
                    null));
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
            var eventMaterialItemRecipeId = random.Next(2) == 0
                ? int.MinValue
                : int.MaxValue;
            Dictionary<int, int> materialsToUse = random.Next(2) == 0
                ? null
                : new Dictionary<int, int>();
            var action = EventMaterialItemCraftsFactory.Create(
                actionTypeIdentifier,
                avatarAddr,
                eventScheduleId,
                eventMaterialItemRecipeId,
                materialsToUse);
            var attr = action.GetType().GetCustomAttribute(typeof(ActionTypeAttribute))
                as ActionTypeAttribute;
            Assert.Equal(actionTypeIdentifier, attr?.TypeIdentifier);
            AssertEquals(
                avatarAddr,
                eventScheduleId,
                eventMaterialItemRecipeId,
                materialsToUse,
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
                EventMaterialItemCraftsFactory.Create(
                    actionTypeIdentifier,
                    avatarAddr,
                    0,
                    0,
                    null));
        }

        private static void AssertEquals(
            Address avatarAddr,
            int eventScheduleId,
            int eventMaterialItemRecipeId,
            Dictionary<int, int> materialsToUse,
            IEventMaterialItemCrafts action)
        {
            Assert.Equal(avatarAddr, action.AvatarAddress);
            Assert.Equal(eventScheduleId, action.EventScheduleId);
            Assert.Equal(
                eventMaterialItemRecipeId,
                action.EventMaterialItemRecipeId);
            if (materialsToUse is null)
            {
                Assert.Null(action.MaterialsToUse);
            }
            else
            {
                Assert.NotNull(action.MaterialsToUse);
                Assert.True(materialsToUse.SequenceEqual(action.MaterialsToUse));
            }
        }
    }
}
