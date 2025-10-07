using System;
using System.Collections;
using Lib9c.Model.Character;

namespace Lib9c.Model.BattleStatus.Arena
{
    [Serializable]
    public class ArenaTurnEnd : ArenaEventBase
    {
        public readonly int TurnNumber;

        public ArenaTurnEnd(ArenaCharacter character,  int turnNumber) : base(character)
        {
            TurnNumber = turnNumber;
        }

        public override IEnumerator CoExecute(IArena arena)
        {
            yield return arena.CoTurnEnd(TurnNumber);
        }
    }
}
