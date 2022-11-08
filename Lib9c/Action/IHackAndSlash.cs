using System;
using System.Collections.Generic;
using Libplanet;

namespace Nekoyume.Action
{
#pragma warning disable S101
    public abstract class IHackAndSlash : GameAction
#pragma warning restore S101
    {
        public abstract List<Guid> Costumes { get; set; }
        public abstract List<Guid> Equipments { get; set; }
        public abstract List<Guid> Foods { get; set; }
        public abstract List<int> Runes { get; set; }
        public abstract int WorldId { get; set; }
        public abstract int StageId { get; set; }
        public abstract int? StageBuffId { get; set; }
        public abstract Address AvatarAddress { get; set; }
        public abstract int PlayCount { get; set; }
    }
}
