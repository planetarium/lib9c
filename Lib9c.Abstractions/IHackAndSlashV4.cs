using System;
using System.Collections.Generic;
using Libplanet.Crypto;

namespace Lib9c.Abstractions
{
    public interface IHackAndSlashV4
    {
        IEnumerable<Guid> Costumes { get; }
        IEnumerable<Guid> Equipments { get; }
        IEnumerable<Guid> Foods { get; }
        int WorldId { get; }
        int StageId { get; }
        int PlayCount { get; }
        Address AvatarAddress { get; }
        Address RankingMapAddress { get; }
    }
}
