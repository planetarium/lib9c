using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.State;
using Nekoyume.Module;

namespace Nekoyume.Action
{
    [ActionType(TypeIdentifier)]
    public class MigrateFee : ActionBase
    {
        public const string TypeIdentifier = "migrate_fee";
        // TODO Will change once target address is determined
        public static readonly Address TargetAddress = new Address();

        public List<Address> FeeAddresses;

        public MigrateFee()
        {
        }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values",
                Dictionary.Empty
                    .Add("f", new List(FeeAddresses.Select(a => a.Serialize())))
                );

        public override void LoadPlainValue(IValue plainValue)
        {
            var dict = (Dictionary)((Dictionary)plainValue)["values"];
            FeeAddresses = ((List)dict["f"]).ToList(a => a.ToAddress());
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);

            CheckPermission(context);
            var states = context.PreviousState;
            var goldCurrency = states.GetGoldCurrency();
            foreach (var address in FeeAddresses)
            {
                var balance = states.GetBalance(address, goldCurrency);
                if (balance > 0 * goldCurrency)
                {
                    states = states.TransferAsset(context, address, TargetAddress, balance);
                }
            }

            return states;
        }
    }
}
