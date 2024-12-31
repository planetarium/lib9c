#nullable enable

using System;
using Lib9c.DevExtensions.Action.Interface;

namespace Lib9c.DevExtensions.Action.Factory
{
    public static class CreateOrReplaceAvatarFactory
    {
        public static (Exception? exception, ICreateOrReplaceAvatar? result)
            TryGetByBlockIndex(
                int avatarIndex = 0,
                string name = "Avatar",
                int hair = 0,
                int lens = 0,
                int ear = 0,
                int tail = 0,
                int level = 1,
                (int equipmentId, int level)[]? equipments = null,
                (int consumableId, int count)[]? foods = null,
                int[]? costumeIds = null,
                (int runeId, int level)[]? runes = null,
                (int stageId, int[] crystalRandomBuffIds)? crystalRandomBuff = null)
        {
            try
            {
                return (
                    null,
                    new CreateOrReplaceAvatar(
                        avatarIndex,
                        name,
                        hair,
                        lens,
                        ear,
                        tail,
                        level,
                        equipments,
                        foods,
                        costumeIds,
                        runes,
                        crystalRandomBuff));
            }
            catch (ArgumentException e)
            {
                return (e, null);
            }
        }
    }
}
