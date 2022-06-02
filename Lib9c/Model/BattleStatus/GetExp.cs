using System;
using System.Collections;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class GetExp : EventBase
    {
        public long Exp { get; }

        public GetExp(StageCharacter stageCharacter, long exp) : base(stageCharacter)
        {
            Exp = exp;
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoGetExp(Exp);
        }
    }
}
