using Nekoyume.Model;
using Nekoyume.TableData;

#nullable disable
namespace Nekoyume.Battle
{
    public interface IStageSimulator : ISimulator
    {
        int StageId { get; }
        EnemySkillSheet EnemySkillSheet { get; }
        CollectionMap ItemMap { get; }
    }
}
