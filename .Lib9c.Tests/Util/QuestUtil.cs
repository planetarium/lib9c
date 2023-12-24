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
            var avatarState = stateV1.GetAvatarState(avatarAddress);
            avatarState.questList = emptyQuestList;
            var newStateV1 = stateV1.SetAvatarState(avatarAddress, avatarState, true, true, true, true);
            var newStateV2 = stateV2.SetAvatarState(avatarAddress, avatarState, false, false, false, true);
            return (newStateV1, newStateV2);
        }
    }
}
