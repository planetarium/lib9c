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
            int? stageBuffId = null,
            int? playCount = 1,
            int? totalPlayCount = 1,
            int? apStoneCount = 0
        )
        {
            if (blockIndex > BlockChain.Policy.BlockPolicySource.V100360ObsoleteIndex)
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
                    StageBuffId = stageBuffId,
                    TotalPlayCount = totalPlayCount ?? 1,
                    ApStoneCount = apStoneCount ?? 0
                };
            }
            if (blockIndex > BlockChain.Policy.BlockPolicySource.V100340ObsoleteIndex)
            {
                return new HackAndSlash19
                {
                    AvatarAddress = avatarAddress,
                    WorldId = worldId,
                    StageId = stageId,
                    Costumes = costumeIds ?? new List<Guid>(),
                    Equipments = equipmentIds ?? new List<Guid>(),
                    Foods = consumableIds ?? new List<Guid>(),
                    RuneInfos = runeSlotInfos ?? new List<RuneSlotInfo>(),
                    StageBuffId = stageBuffId,
                    PlayCount = playCount ?? 1,
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
                StageBuffId = stageBuffId,
                PlayCount = playCount ?? 1,
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
            int? stageBuffId = null,
            int? playCount = 1,
            int? totalPlayCount = 1,
            int? apStoneCount = 0
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
                StageBuffId = stageBuffId,
                PlayCount = playCount ?? 1,
            },
            19 => new HackAndSlash19
            {
                AvatarAddress = avatarAddress,
                WorldId = worldId,
                StageId = stageId,
                Costumes = costumeIds ?? new List<Guid>(),
                Equipments = equipmentIds ?? new List<Guid>(),
                Foods = consumableIds ?? new List<Guid>(),
                RuneInfos = runeSlotInfos ?? new List<RuneSlotInfo>(),
                StageBuffId = stageBuffId,
                PlayCount = playCount ?? 1,
            },
            20 => new HackAndSlash
            {
                AvatarAddress = avatarAddress,
                WorldId = worldId,
                StageId = stageId,
                Costumes = costumeIds ?? new List<Guid>(),
                Equipments = equipmentIds ?? new List<Guid>(),
                Foods = consumableIds ?? new List<Guid>(),
                RuneInfos = runeSlotInfos ?? new List<RuneSlotInfo>(),
                StageBuffId = stageBuffId,
                TotalPlayCount = totalPlayCount ?? 1,
                ApStoneCount = apStoneCount ?? 0,
            },
            _ => throw new ArgumentOutOfRangeException($"Invalid version: {version}"),
        };
    }
}
