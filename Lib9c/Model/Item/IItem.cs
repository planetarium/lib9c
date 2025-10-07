using Lib9c.Model.State;

namespace Lib9c.Model.Item
{
    public interface IItem: IState
    {
        ItemType ItemType { get; }

        ItemSubType ItemSubType { get; }
    }
}
