using System;
using System.Collections;

#nullable disable
namespace Nekoyume.Model.BattleStatus
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
