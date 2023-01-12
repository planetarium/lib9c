using System;
using System.Collections;
using Lib9c.Model.Character;

namespace Lib9c.Model.BattleStatus
{
    [Serializable]
    public class RemoveBuffs : EventBase
    {
        public RemoveBuffs(CharacterBase character) : base(character)
        {
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoRemoveBuffs(Character);
        }
    }
}
