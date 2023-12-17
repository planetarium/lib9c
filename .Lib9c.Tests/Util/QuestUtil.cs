namespace Lib9c.Tests.Util
{
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
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
            var newStateV1 = stateV1.SetState(avatarAddress, avatarState.Serialize());
            var newStateV2 = stateV2.SetState(
                avatarAddress.Derive(SerializeKeys.LegacyQuestListKey),
                emptyQuestList.Serialize()
            );
            return (newStateV1, newStateV2);
        }
    }
}
