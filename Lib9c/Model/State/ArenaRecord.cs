using System;
using System.Collections.Generic;
using Bencodex.Types;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.State
{
    public class ArenaRecord : IState
    {
        public int Win { get; private set; }
        public int Lose { get; private set; }
        public int Score { get; private set; }

        public ArenaRecord()
        {
            Score = GameConfig.ArenaScoreDefault;
        }

        public ArenaRecord(List serialized)
        {
            Win = (Integer)serialized[0];
            Lose = (Integer)serialized[1];
            Score = (Integer)serialized[2];
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(Win)
                .Add(Lose)
                .Add(Score);
        }

        public void Update(int score, bool isWin)
        {
            Score = Math.Max(score, GameConfig.ArenaScoreDefault);

            if (isWin)
            {
                Win++;
            }
            else
            {
                Lose++;
            }
        }
    }
}
