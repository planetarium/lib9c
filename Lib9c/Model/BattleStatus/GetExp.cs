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

        public override IEnumerator CoExecute(IWorld world)
        {
            if (world is IStage stage)
            {
                yield return stage.CoGetExp(Exp);
            }
            else
            {
                throw new InvalidCastException(nameof(world));
            }
        }
    }
}
