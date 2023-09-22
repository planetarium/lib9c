namespace Lib9c.Tests.Util
{
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Model.Quest;
    using Nekoyume.Module;
    using Nekoyume.TableData;

    public static class QuestUtil
    {
        public static IWorld DisableQuestList(
            IWorld state,
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
            var avatarState = AvatarModule.GetAvatarState(state, avatarAddress);
            avatarState.questList = emptyQuestList;
            var newState = AvatarModule.SetAvatarState(
                state,
                avatarAddress,
                avatarState,
                true,
                true,
                true,
                true);
            return newState;
        }
    }
}
