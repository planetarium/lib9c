using System;
using System.Collections;

#nullable disable
namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class Dead : EventBase
    {
        public Dead(CharacterBase character) : base(character)
        {
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoDead(Character);
        }
    }
}
