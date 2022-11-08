using System;
using System.Collections.Generic;
using Libplanet;

namespace Nekoyume.Action
{
    public static class HackAndSlashFactory
    {
        public static GameAction HackAndSlash(
            long blockIndex,
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
            if (blockIndex > 1L)
            {
                return (GameAction)HackAndSlash(costumes, equipments, foods, runes, worldId, stageId,
                    avatarAddress, playCount, stageBuffId);
            }

            return (GameAction)HackAndSlash18(costumes, equipments, foods, worldId, stageId, avatarAddress,
                playCount, stageBuffId);
        }

        public static IHackAndSlash HackAndSlash(
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

        public static IHackAndSlash18 HackAndSlash18(
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
