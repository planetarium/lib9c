using System;
using System.Numerics;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.State
{
    /// <summary>
    /// Represents the state of a World Boss in the game.
    /// </summary>
    [Serializable]
    public class WorldBossState : IState
    {
        /// <summary>
        /// The boss id of the World Boss from <see cref="WorldBossListSheet"/>.
        /// </summary>
        public int Id;

        /// <summary>
        /// Current boss level.
        /// </summary>
        public int Level;

        /// <summary>
        /// Current hit points of the World Boss.
        /// </summary>
        public BigInteger CurrentHp;

        /// <summary>
        /// Block index when the World Boss season started.
        /// </summary>
        public long StartedBlockIndex;

        /// <summary>
        /// Block index when the World Boss season ended.
        /// </summary>
        public long EndedBlockIndex;

        /// <summary>
        /// Total damage dealt to the World Boss in season.
        /// </summary>
        public BigInteger TotalDamage;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorldBossState"/> class using the specified row data.
        /// </summary>
        /// <param name="row">The row containing the World Boss data.</param>
        /// <param name="hpRow">The row containing the World Boss HP data.</param>
        public WorldBossState(WorldBossListSheet.Row row, WorldBossGlobalHpSheet.Row hpRow)
        {
            // Fenrir Id.
            Id = row.BossId;
            Level = 1;
            CurrentHp = hpRow.Hp;
            StartedBlockIndex = row.StartedBlockIndex;
            EndedBlockIndex = row.EndedBlockIndex;
            TotalDamage = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorldBossState"/> class from serialized data.
        /// </summary>
        /// <param name="serialized">The serialized data for the World Boss state.</param>
        public WorldBossState(List serialized)
        {
            Id = serialized[0].ToInteger();
            Level = serialized[1].ToInteger();
            CurrentHp = serialized[2].ToBigInteger();
            StartedBlockIndex = serialized[3].ToLong();
            EndedBlockIndex = serialized[4].ToLong();
            // Handle deserialize legacy state.
            TotalDamage = serialized.Count > 5 ? serialized[5].ToBigInteger() : BigInteger.Zero;
        }

        /// <summary>
        /// Serializes the current state of the World Boss into a serializable format.
        /// </summary>
        /// <returns>A serialized representation of the World Boss state.</returns>
        public IValue Serialize()
        {
            return List.Empty
                .Add(Id.Serialize())
                .Add(Level.Serialize())
                .Add(CurrentHp.Serialize())
                .Add(StartedBlockIndex.Serialize())
                .Add(EndedBlockIndex.Serialize())
                .Add(TotalDamage.Serialize());
        }
    }
}
