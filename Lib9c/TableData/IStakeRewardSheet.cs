using System.Collections.Generic;

#nullable disable
namespace Nekoyume.TableData
{
    public interface IStakeRewardSheet : ISheet
    {
        IReadOnlyList<IStakeRewardRow> OrderedRows { get; }
    }
}
