using System.Collections.Generic;

namespace Lib9c.TableData
{
    public interface IWorldBossRewardSheet : ISheet
    {
        IReadOnlyList<IWorldBossRewardRow> OrderedRows { get; }
    }
}
