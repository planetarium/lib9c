#nullable enable

using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Action.Interface;

namespace Nekoyume.Action.Factory
{
    public static class HackAndSlashFactory
    {
        public static IHackAndSlash CreateByBlockIndex(
            long blockIndex,
            Address avatarAddress,
            int worldId,
            int stageId,
            List<Guid>? costumeIds = null,
            List<Guid>? equipmentIds = null,
            List<Guid>? consumableIds = null,
            List<RuneSlotInfo>? runeSlotInfos = null,
            int? stageBuffId = null
        )
        {
            if (blockIndex > BlockChain.Policy.BlockPolicySource.V100340ObsoleteIndex)
            {
                return new HackAndSlash
                {
                    AvatarAddress = avatarAddress,
                    WorldId = worldId,
                    StageId = stageId,
                    Costumes = costumeIds ?? new List<Guid>(),
                    Equipments = equipmentIds ?? new List<Guid>(),
                    Foods = consumableIds ?? new List<Guid>(),
                    RuneInfos = runeSlotInfos ?? new List<RuneSlotInfo>(),
                    StageBuffId = stageBuffId
                };
            }

            return new HackAndSlash18
            {
                AvatarAddress = avatarAddress,
                WorldId = worldId,
                StageId = stageId,
                Costumes = costumeIds ?? new List<Guid>(),
                Equipments = equipmentIds ?? new List<Guid>(),
                Foods = consumableIds ?? new List<Guid>(),
                StageBuffId = stageBuffId
            };
        }

        public static IHackAndSlash CreateByVersion(
            int version,
            Address avatarAddress,
            int worldId,
            int stageId,
            List<Guid>? costumeIds = null,
            List<Guid>? equipmentIds = null,
            List<Guid>? consumableIds = null,
            List<RuneSlotInfo>? runeSlotInfos = null,
            int? stageBuffId = null
        ) => version switch
        {
            18 => new HackAndSlash18
            {
                AvatarAddress = avatarAddress,
                WorldId = worldId,
                StageId = stageId,
                Costumes = costumeIds ?? new List<Guid>(),
                Equipments = equipmentIds ?? new List<Guid>(),
                Foods = consumableIds ?? new List<Guid>(),
                StageBuffId = stageBuffId
            },
            19 => new HackAndSlash
            {
                AvatarAddress = avatarAddress,
                WorldId = worldId,
                StageId = stageId,
                Costumes = costumeIds ?? new List<Guid>(),
                Equipments = equipmentIds ?? new List<Guid>(),
                Foods = consumableIds ?? new List<Guid>(),
                RuneInfos = runeSlotInfos ?? new List<RuneSlotInfo>(),
                StageBuffId = stageBuffId
            },
            _ => throw new ArgumentOutOfRangeException($"Invalid version: {version}"),
        };
    }
}
