using Nekoyume.Model.Elemental;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Item
{
    public interface IItem: IState
    {
        int Id { get; }
        
        int Grade { get; }
        
        ItemType ItemType { get; }

        ItemSubType ItemSubType { get; }
        
        ElementalType ElementalType { get; }
    }
}
