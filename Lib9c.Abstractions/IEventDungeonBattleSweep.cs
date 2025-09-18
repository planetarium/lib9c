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
        Address AvatarAddress { get; }
        int EventScheduleId { get; }
        int EventDungeonId { get; }
        int EventDungeonStageId { get; }
        IEnumerable<Guid> Equipments { get; }
        IEnumerable<Guid> Costumes { get; }
        IEnumerable<Guid> Foods { get; }
        IEnumerable<IValue> RuneSlotInfos { get; }
        int PlayCount { get; }
    }
}
