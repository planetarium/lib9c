using System;
using System.Collections;
using Nekoyume.Model.Character;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public abstract class EventBase
    {
        public readonly ICharacter Character;

        protected EventBase(ICharacter character)
        {
            Character = character;
        }

        public abstract IEnumerator CoExecute(IWorld world);
    }
}
