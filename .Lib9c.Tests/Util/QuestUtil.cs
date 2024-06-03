namespace Lib9c.Tests.Util
{
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Model.Quest;
    using Nekoyume.Module;
    using Nekoyume.TableData;

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
