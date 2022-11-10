using System;
using System.Collections.Generic;
using Libplanet;

namespace Nekoyume.Action
{
    public static class HackAndSlashFactory
    {
        public static GameAction HackAndSlash(
            int version,
            List<Guid> costumes,
            List<Guid> equipments,
            List<Guid> foods,
            List<int> runes,
            int worldId,
            int stageId,
            Address avatarAddress,
            int playCount = 1,
            int? stageBuffId = null
        )
        {
            switch (version)
            {
                case 18:
                    return HackAndSlash18(costumes, equipments, foods, worldId, stageId, avatarAddress,
                        playCount, stageBuffId);
                case 19:
                    return HackAndSlash19(costumes, equipments, foods, runes, worldId, stageId,
                        avatarAddress, playCount, stageBuffId);
                default:
                    return HackAndSlash20(costumes, equipments, foods, runes, worldId, stageId,
                        avatarAddress, playCount, stageBuffId);
            }
        }

        private static GameAction HackAndSlash20(
            List<Guid> costumes,
            List<Guid> equipments,
            List<Guid> foods,
            List<int> runes,
            int worldId,
            int stageId,
            Address avatarAddress,
            int playCount = 1,
            int? stageBuffId = null
        )
        {
            return new HackAndSlash20
            {
                Costumes = costumes,
                Equipments = equipments,
                Foods = foods,
                Runes = runes,
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = avatarAddress,
                PlayCount = playCount,
                StageBuffId = stageBuffId
            };
        }

        private static GameAction HackAndSlash19(
            List<Guid> costumes,
            List<Guid> equipments,
            List<Guid> foods,
            List<int> runes,
            int worldId,
            int stageId,
            Address avatarAddress,
            int playCount = 1,
            int? stageBuffId = null
        )
        {
            return new HackAndSlash
            {
                Costumes = costumes,
                Equipments = equipments,
                Foods = foods,
                Runes = runes,
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = avatarAddress,
                PlayCount = playCount,
                StageBuffId = stageBuffId
            };
        }

        private static GameAction HackAndSlash18(
            List<Guid> costumes,
            List<Guid> equipments,
            List<Guid> foods,
            int worldId,
            int stageId,
            Address avatarAddress,
            int playCount = 1,
            int? stageBuffId = null
        )
        {
            return new HackAndSlash18
            {
                Costumes = costumes,
                Equipments = equipments,
                Foods = foods,
                WorldId = worldId,
                StageId = stageId,
                AvatarAddress = avatarAddress,
                PlayCount = playCount,
                StageBuffId = stageBuffId
            };
        }
    }
}
