using Nekoyume.TableData;

#nullable disable
namespace Nekoyume.Model.Skill
{
    public interface ISkill
    {
        public SkillSheet.Row SkillRow { get; }
        public int Power { get; }
        public int Chance { get; }
        public void Update(int chance, int power);
    }
}
