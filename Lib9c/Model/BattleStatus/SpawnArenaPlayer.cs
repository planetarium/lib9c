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

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoSpawnArenaPlayer((ArenaCharacter)Character);
        }
    }
}
