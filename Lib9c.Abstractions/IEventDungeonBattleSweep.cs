using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Crypto;

namespace Lib9c.Abstractions
{
    /// <summary>
    /// Interface for EventDungeonBattleSweep action.
    /// Represents a sweep action that automatically completes multiple event dungeon battles without player interaction.
    /// </summary>
    public interface IEventDungeonBattleSweep
    {
        /// <summary>
        /// The address of the avatar that will participate in the event dungeon battle.
        /// </summary>
        Address AvatarAddress { get; }

        /// <summary>
        /// The ID of the event schedule that defines when the event dungeon is available.
        /// </summary>
        int EventScheduleId { get; }

        /// <summary>
        /// The ID of the event dungeon to battle in.
        /// </summary>
        int EventDungeonId { get; }

        /// <summary>
        /// The ID of the specific stage within the event dungeon to battle.
        /// </summary>
        int EventDungeonStageId { get; }

        /// <summary>
        /// A collection of equipment item GUIDs to be equipped for the battle.
        /// </summary>
        IEnumerable<Guid> Equipments { get; }

        /// <summary>
        /// A collection of costume item GUIDs to be equipped for the battle.
        /// </summary>
        IEnumerable<Guid> Costumes { get; }

        /// <summary>
        /// A collection of food item GUIDs to be consumed during the battle.
        /// </summary>
        IEnumerable<Guid> Foods { get; }

        /// <summary>
        /// A collection of rune slot information serialized as IValue.
        /// Each item represents a rune slot configuration for the battle.
        /// </summary>
        IEnumerable<IValue> RuneSlotInfos { get; }

        /// <summary>
        /// The number of times to play the battle in a single sweep action.
        /// </summary>
        int PlayCount { get; }
    }
}
