using System;
using System.Collections;
using Lib9c.Model.Character;

namespace Lib9c.Model.BattleStatus
{
    [Serializable]
    public class GetExp : EventBase
    {
        public long Exp { get; }

        public GetExp(CharacterBase character, long exp) : base(character)
        {
            Exp = exp;
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoGetExp(Exp);
        }
    }
}
