using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Crypto;

namespace Nekoyume.Action
{
    /// <summary>
    /// Interface for hack and slash action version 10.
    /// </summary>
    public interface IHackAndSlashV10
    {
        /// <summary>
        /// Gets the costume IDs to use in battle.
        /// </summary>
        IEnumerable<Guid> Costumes { get; }

        /// <summary>
        /// Gets the equipment IDs to use in battle.
        /// </summary>
        IEnumerable<Guid> Equipments { get; }

        /// <summary>
        /// Gets the food IDs to use in battle.
        /// </summary>
        IEnumerable<Guid> Foods { get; }

        /// <summary>
        /// Gets the rune slot information for the battle.
        /// </summary>
        IEnumerable<IValue> RuneSlotInfos { get; }

        /// <summary>
        /// Gets the world ID for the stage.
        /// </summary>
        int WorldId { get; }

        /// <summary>
        /// Gets the stage ID to challenge.
        /// </summary>
        int StageId { get; }

        /// <summary>
        /// Gets the stage buff ID to apply.
        /// </summary>
        int? StageBuffId { get; }

        /// <summary>
        /// Gets the total number of plays.
        /// </summary>
        int TotalPlayCount { get; }

        /// <summary>
        /// Gets the number of AP stones to use.
        /// </summary>
        int ApStoneCount { get; }

        /// <summary>
        /// Gets the avatar address for the battle.
        /// </summary>
        Address AvatarAddress { get; }
    }
}
