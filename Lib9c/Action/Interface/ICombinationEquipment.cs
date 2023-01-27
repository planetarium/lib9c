using Libplanet;
using Libplanet.Action;

namespace Nekoyume.Action.Interface
{
    public interface ICombinationEquipmentFamily: IAction
    {
    }

    public interface ICombinationEquipment : ICombinationEquipmentFamily
    {
        Address AvatarAddress { get; }
        int SlotIndex { get; }
        int RecipeId { get; }
        int? SubRecipeId { get; }
    }

    public interface ICombinationEquipmentV2 : ICombinationEquipmentFamily
    {
        Address AvatarAddress { get; }
        int SlotIndex { get; }
        int RecipeId { get; }
        int? SubRecipeId { get; }
        bool PayByCrystal { get; }

    }
    public interface ICombinationEquipmentV3 : ICombinationEquipmentFamily
    {
        Address AvatarAddress { get; }
        int SlotIndex { get; }
        int RecipeId { get; }
        int? SubRecipeId { get; }
        bool PayByCrystal { get; }
        bool UseHammerPoint { get; }
    }
}
