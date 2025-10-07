using System.Collections.Generic;

namespace Lib9c.TableData
{
    public interface IStakeRewardSheet : ISheet
    {
        IReadOnlyList<IStakeRewardRow> OrderedRows { get; }
    }
}
