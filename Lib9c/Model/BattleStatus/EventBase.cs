using System;
using System.Collections;
using Lib9c.Model.Character;

namespace Lib9c.Model.BattleStatus
{
    [Serializable]
    public abstract class EventBase
    {
        public readonly CharacterBase Character;

        protected EventBase(CharacterBase character)
        {
            Character = character;
        }

        public abstract IEnumerator CoExecute(IStage stage);
    }
}
