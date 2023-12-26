namespace Lib9c.Tests.Util
{
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Model.Quest;
    using Nekoyume.Module;
    using Nekoyume.TableData;

    public static class QuestUtil
    {
        public static (IWorld, IWorld) DisableQuestList(
            IWorld stateV1,
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
            var avatarState1 = stateV1.GetAvatarState(avatarAddress);
            var avatarState2 = stateV2.GetAvatarState(avatarAddress);
            avatarState1.questList = emptyQuestList;
            avatarState2.questList = emptyQuestList;
            var newStateV1 = stateV1.SetAvatarState(avatarAddress, avatarState1, false, false, false, true);
            var newStateV2 = stateV2.SetAvatarState(avatarAddress, avatarState2, false, false, false, true);
            return (newStateV1, newStateV2);
        }
    }
}
