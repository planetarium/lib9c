using System;
using System.Collections;
using System.Collections.Generic;
using Lib9c.Model.Character;
using Lib9c.TableData.AdventureBoss;

namespace Lib9c.Model.BattleStatus.AdventureBoss
{
    [Serializable]
    public class Breakthrough : EventBase
    {
        public readonly int FloorId;
        public readonly List<AdventureBossFloorWaveSheet.MonsterData> Monsters;

        public Breakthrough(CharacterBase character, int floorId,
            List<AdventureBossFloorWaveSheet.MonsterData> monsters
            ) : base(character)
        {
            FloorId = floorId;
            Monsters = monsters;
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoBreakthrough((Player)Character, FloorId, Monsters);
        }
    }
}
