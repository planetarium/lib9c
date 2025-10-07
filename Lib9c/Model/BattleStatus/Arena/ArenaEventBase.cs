using System;
using System.Collections;
using Lib9c.Model.Character;

namespace Lib9c.Model.BattleStatus.Arena
{
    [Serializable]
    public abstract class ArenaEventBase
    {
        public readonly ArenaCharacter Character;

        protected ArenaEventBase(ArenaCharacter character)
        {
            Character = character;
        }

        public abstract IEnumerator CoExecute(IArena arena);
    }
}
