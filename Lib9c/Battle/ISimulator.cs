using System.Collections.Generic;
using Lib9c.Model.BattleStatus;
using Lib9c.Model.Character;
using Lib9c.Model.Item;
using Priority_Queue;

namespace Lib9c.Battle
{
    public interface ISimulator
    {
        Player Player { get; }
        BattleLog Log { get; }
        SimplePriorityQueue<CharacterBase, decimal> Characters { get; }
        IEnumerable<ItemBase> Reward { get; }
        int WaveNumber { get; }
        int WaveTurn { get; }
        bool LogEvent { get; }
    }
}
