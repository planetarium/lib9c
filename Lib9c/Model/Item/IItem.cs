using Nekoyume.Model.State;

#nullable disable
namespace Nekoyume.Model.Item
{
    public interface IItem: IState
    {
        ItemType ItemType { get; }

        ItemSubType ItemSubType { get; }
    }
}
