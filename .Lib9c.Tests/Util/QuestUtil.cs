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
            var avatarState = AvatarModule.GetAvatarState(stateV1, avatarAddress);
            avatarState.questList = emptyQuestList;
            var newStateV1 = LegacyModule.SetState(stateV1, avatarAddress, avatarState.Serialize());
            var newStateV2 = LegacyModule.SetState(
                stateV2,
                avatarAddress.Derive(SerializeKeys.LegacyQuestListKey),
                emptyQuestList.Serialize()
            );
            return (newStateV1, newStateV2);
        }
    }
}
