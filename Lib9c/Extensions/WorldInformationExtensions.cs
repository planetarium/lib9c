using Nekoyume.Action;
using Nekoyume.Model;

#nullable disable
namespace Nekoyume.Extensions
{
    public static class WorldInformationExtensions
    {
        public static void ValidateFromAction(
            this WorldInformation worldInformation,
            int stageId,
            string actionTypeText,
            string addressesHex)
        {
            if (worldInformation.IsStageCleared(stageId))
            {
                return;
            }

            worldInformation.TryGetLastClearedStageId(out var current);
            throw new NotEnoughClearedStageLevelException(
                actionTypeText,
                addressesHex,
                stageId,
                current);
        }
    }
}
