using Lib9c.Model;
using Lib9c.TableData.Skill;

namespace Lib9c.Battle
{
    public interface IStageSimulator : ISimulator
    {
        int StageId { get; }
        EnemySkillSheet EnemySkillSheet { get; }
        CollectionMap ItemMap { get; }
    }
}
