using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.TableData.AdventureBoss;

namespace Nekoyume.Model.BattleStatus.AdventureBoss
{
    [Serializable]
    public class Breakthrough : EventBase
    {
        public readonly int Floor;
        public readonly List<FloorWaveSheet.MonsterData> Monsters;

        public Breakthrough(CharacterBase character, int floor,
            List<FloorWaveSheet.MonsterData> monsters
            ) : base(character)
        {
            Floor = floor;
            Monsters = monsters;
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoBreakthrough((Player)Character, Floor, Monsters);
        }
    }
}
