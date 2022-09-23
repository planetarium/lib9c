using System;
using System.Collections;

#nullable disable
namespace Nekoyume.Model.BattleStatus
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
