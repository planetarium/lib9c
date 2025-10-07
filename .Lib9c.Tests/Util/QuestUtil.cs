namespace Lib9c.Tests.Util
{
    using Lib9c.Model.Quest;
    using Lib9c.Module;
    using Lib9c.TableData.Item;
    using Lib9c.TableData.Quest;
    using Libplanet.Action.State;
    using Libplanet.Crypto;

    public static class QuestUtil
    {
        public static IWorld DisableQuestList(
            IWorld stateV2,
            Address avatarAddress
        )
        {
            var emptyQuestList = new QuestList(
                new QuestSheet(),
                new QuestRewardSheet(),
                new QuestItemRewardSheet(),
                new EquipmentItemRecipeSheet(),
                new EquipmentItemSubRecipeSheet()
            );
            var avatarState2 = stateV2.GetAvatarState(avatarAddress);
            avatarState2.questList = emptyQuestList;
            var newStateV2 = stateV2.SetAvatarState(avatarAddress, avatarState2);
            return newStateV2;
        }
    }
}
