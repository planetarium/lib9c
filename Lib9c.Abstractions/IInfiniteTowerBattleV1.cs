using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Crypto;

namespace Lib9c.Abstractions
{
    /// <summary>
    /// Interface for infinite tower battle action.
    /// </summary>
    public interface IInfiniteTowerBattleV1
    {
        /// <summary>
        /// Gets the avatar address for the battle.
        /// </summary>
        Address AvatarAddress { get; }

        /// <summary>
        /// Gets the infinite tower ID.
        /// </summary>
        int InfiniteTowerId { get; }

        /// <summary>
        /// Gets the floor ID to challenge.
        /// </summary>
        int FloorId { get; }

        /// <summary>
        /// Gets the equipment IDs to use in battle.
        /// </summary>
        IEnumerable<Guid> Equipments { get; }

        /// <summary>
        /// Gets the costume IDs to use in battle.
        /// </summary>
        IEnumerable<Guid> Costumes { get; }

        /// <summary>
        /// Gets the food IDs to use in battle.
        /// </summary>
        IEnumerable<Guid> Foods { get; }

        /// <summary>
        /// Gets the rune slot information for the battle.
        /// </summary>
        IEnumerable<IValue> RuneSlotInfos { get; }

        /// <summary>
        /// Gets whether to buy a ticket if needed.
        /// </summary>
        bool BuyTicketIfNeeded { get; }
    }
}
