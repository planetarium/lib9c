using System;
using System.Collections;
using Nekoyume.Model.Character;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class SpawnPlayer : EventBase
    {
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoSpawnPlayer((Player)Character);
        }

        public SpawnPlayer(ICharacter character) : base(character)
        {
        }
    }
}
