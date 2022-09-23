using System.Collections.Generic;

#nullable disable
namespace Nekoyume.TableData
{
    public interface IWorldBossRewardSheet : ISheet
    {
        IReadOnlyList<IWorldBossRewardRow> OrderedRows { get; }
    }
}
