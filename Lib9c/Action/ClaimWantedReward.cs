using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [ActionType(TypeIdentifier)]
    public class ClaimWantedReward : ActionBase
    {
        public const string TypeIdentifier = "claim_wanted_reward";
        public long Season;
        public Address AvatarAddress;
        public override IValue PlainValue =>
        Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", List.Empty.Add(Season).Add(AvatarAddress.Serialize()));
        public override void LoadPlainValue(IValue plainValue)
        {
            var list = (List) ((Dictionary) plainValue)["values"];
            Season = (Integer) list[0];
            AvatarAddress = list[1].ToAddress();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            var inventory = states.GetInventory(AvatarAddress);
            var materialItemSheet = states.GetSheet<MaterialItemSheet>();
            var material = ItemFactory.CreateMaterial(materialItemSheet.Values.First());
            inventory.AddItem(material);
            return states.SetInventory(AvatarAddress, inventory);
        }
    }
}
