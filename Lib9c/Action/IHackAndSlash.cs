using System;
using System.Collections.Generic;
using Libplanet;
using Libplanet.Action;

namespace Nekoyume.Action
{
    public interface IHackAndSlash : IAction
    {
        public Guid Id { get; }
        public List<Guid> Costumes { get; set; }
        public List<Guid> Equipments { get; set; }
        public List<Guid> Foods { get; set; }
        public int WorldId { get; set; }
        public int StageId { get; set; }
        public int? StageBuffId { get; set; }
        public Address AvatarAddress { get; set; }
        public int PlayCount { get; set; }
    }
}
