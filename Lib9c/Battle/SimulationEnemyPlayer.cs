using System.Collections.Generic;
using Nekoyume.Model;
using Nekoyume.Model.Item;

namespace Nekoyume.Battle
{
    public readonly struct SimulationEnemyPlayer
    {
        public readonly string NameWithHash;
        public readonly int CharacterId;
        public readonly int Level;
        public readonly int HairIndex;
        public readonly int LensIndex;
        public readonly int EarIndex;
        public readonly int TailIndex;

        public readonly IReadOnlyList<Costume> Costumes;
        public readonly IReadOnlyList<Equipment> Equipments;

        public SimulationEnemyPlayer(EnemyPlayer enemyPlayer)
        {
            NameWithHash = enemyPlayer.NameWithHash;
            CharacterId = enemyPlayer.CharacterId;
            Level = enemyPlayer.Level;
            HairIndex = enemyPlayer.hairIndex;
            LensIndex = enemyPlayer.hairIndex;
            EarIndex = enemyPlayer.earIndex;
            TailIndex = enemyPlayer.tailIndex;
            Costumes = enemyPlayer.Costumes;
            Equipments = enemyPlayer.Equipments;
        }
    }
}
