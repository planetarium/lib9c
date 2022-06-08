using System;
using System.Collections;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class SpawnEnemyPlayer : EventBase
    {
        public SpawnEnemyPlayer(StageCharacter stageCharacter) : base(stageCharacter)
        {
        }

        public override IEnumerator CoExecute(IWorld world)
        {
            if (world is IStage stage)
            {
                yield return stage.CoSpawnEnemyPlayer((EnemyPlayer)Character);
            }
            else
            {
                throw new InvalidCastException(nameof(world));
            }
        }
    }
}
