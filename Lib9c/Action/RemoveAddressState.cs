using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [ActionType(TypeId)]
    public class RemoveAddressState : ActionBase
    {
        public const string TypeId = "remove_address_state";

        public IReadOnlyList<(Address accountAddress, Address targetAddress)> Removals { get; private set; }

        public RemoveAddressState()
        {
        }

        public RemoveAddressState(IReadOnlyList<(Address accountAddress, Address targetAddress)> removals)
        {
            Removals = removals;
        }

        public override IValue PlainValue =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                { (Text)"type_id", (Text)TypeId },
                {
                    (Text)"values", Dictionary.Empty
                    .Add("r", new List(Removals.Select(r =>
                        List.Empty
                            .Add(r.accountAddress.Serialize())
                            .Add(r.targetAddress.Serialize()))))
                }
            });

        public override void LoadPlainValue(IValue plainValue)
        {
            var dictionary = (Dictionary)plainValue;
            var values = (Dictionary)dictionary["values"];
            Removals = ((List)values["r"])
                .Select(r =>
                {
                    var list = (List)r;
                    return (list[0].ToAddress(), list[1].ToAddress());
                })
                .ToList();
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            CheckPermission(context);

            foreach (var (accountAddress, targetAddress) in Removals)
            {
                var account = states.GetAccount(accountAddress);
                account = account.RemoveState(targetAddress);
                states = states.SetAccount(accountAddress, account);
            }
            return states;
        }
    }
}
