using System.Collections;
using Nekoyume.Model.Character;

namespace Nekoyume.Model.BattleStatus
{
    public class ArenaTurnEnd : EventBase
    {
        public readonly int TurnNumber;
        public ArenaTurnEnd(ICharacter character, int turnNumber) : base(character)
        {
            TurnNumber = turnNumber;
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoArenaTurnEnd(TurnNumber);
        }
    }
}
