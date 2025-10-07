using System;
using System.Collections;
using Lib9c.Model.Character;

namespace Lib9c.Model.BattleStatus
{
    [Serializable]
    public class SpawnEnemyPlayer : EventBase
    {
        public SpawnEnemyPlayer(CharacterBase character) : base(character)
        {
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoSpawnEnemyPlayer((EnemyPlayer)Character);
        }
    }
}
