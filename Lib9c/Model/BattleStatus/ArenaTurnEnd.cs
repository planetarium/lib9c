using System;
using System.Collections;

namespace Nekoyume.Model.BattleStatus
{
    public class ArenaTurnEnd : EventBase
    {
        public readonly int TurnNumber;

        public ArenaTurnEnd(ICharacter character, int turnNumber) : base(character)
        {
            TurnNumber = turnNumber;
        }

        public override IEnumerator CoExecute(IWorld world)
        {
            if (world is IArena arena)
            {
                yield return arena.CoArenaTurnEnd(TurnNumber);
            }
            else
            {
                throw new InvalidCastException(nameof(world));
            }
        }
    }
}
