using System;
using System.Collections;
using Lib9c.Model.Character;

namespace Lib9c.Model.BattleStatus
{
    [Serializable]
    public class SpawnPlayer : EventBase
    {
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoSpawnPlayer((Player)Character);
        }

        public SpawnPlayer(CharacterBase character) : base(character)
        {
        }
    }
}
