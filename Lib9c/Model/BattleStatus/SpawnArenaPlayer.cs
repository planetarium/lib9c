using System;
using System.Collections;
using Nekoyume.Model.Character;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class SpawnArenaPlayer : EventBase
    {
        public SpawnArenaPlayer(ICharacter character) : base(character)
        {
        }

        public override IEnumerator CoExecute(IWorld world)
        {
            if (world is IArena arena)
            {
                yield return arena.CoSpawnArenaPlayer((ArenaCharacter)Character);
            }
            else
            {
                throw new InvalidCastException(nameof(world));
            }
        }
    }
}
