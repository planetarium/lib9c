using System;
using System.Collections.Generic;
using Libplanet;
using Libplanet.Action;

namespace Nekoyume.Action
{
    public interface IHackAndSlash
    {
        List<Guid> Costumes { get; set; }
        List<Guid> Equipments { get; set; }
        List<Guid> Foods { get; set; }
        List<int> Runes { get; set; }
        int WorldId { get; set; }
        int StageId { get; set; }
        int? StageBuffId { get; set; }
        Address AvatarAddress { get; set; }
        int PlayCount { get; set; }
    }
}
