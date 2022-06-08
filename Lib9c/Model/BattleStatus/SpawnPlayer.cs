using System;
using System.Collections;
using Nekoyume.Model.Character;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class SpawnPlayer : EventBase
    {
        public SpawnPlayer(ICharacter character) : base(character)
        {
        }

        public override IEnumerator CoExecute(IWorld world)
        {
            if (world is IStage stage)
            {
                yield return stage.CoSpawnPlayer((Player)Character);
            }
            else
            {
                throw new InvalidCastException(nameof(world));
            }
        }
    }
}
