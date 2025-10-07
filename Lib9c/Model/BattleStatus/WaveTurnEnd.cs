using System;
using System.Collections;
using Lib9c.Model.Character;

namespace Lib9c.Model.BattleStatus
{
    [Serializable]
    public class WaveTurnEnd : EventBase
    {
        public readonly int TurnNumber;
        public readonly int WaveTurn;

        public WaveTurnEnd(CharacterBase character, int turnNumber, int waveTurn) : base(character)
        {
            TurnNumber = turnNumber;
            WaveTurn = waveTurn;
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoWaveTurnEnd(TurnNumber, WaveTurn);
        }
    }
}
