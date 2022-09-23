using System;
using System.Collections;

#nullable disable
namespace Nekoyume.Model.BattleStatus.Arena
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
