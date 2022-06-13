using System.Collections;
using System.Collections.Generic;
using Nekoyume.Model.Item;

namespace Nekoyume.Model
{
    public interface IStage : IWorld
    {
        IEnumerator CoSpawnPlayer(Player character);
        IEnumerator CoSpawnEnemyPlayer(EnemyPlayer character);
        IEnumerator CoDropBox(List<ItemBase> items);
        IEnumerator CoSpawnWave(int waveNumber, int waveTurn, List<Enemy> enemies, bool hasBoss);
        IEnumerator CoGetExp(long exp);
        IEnumerator CoGetReward(List<ItemBase> rewards);
        IEnumerator CoWaveTurnEnd(int turnNumber, int waveTurn);
    }
}
