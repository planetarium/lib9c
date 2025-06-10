using System.Collections.Generic;
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
        public Address AccountAddress { get; private set; }
        public Address TargetAddress { get; private set; }

        public RemoveAddressState()
        {
        }

        public RemoveAddressState(Address accountAddress, Address targetAddress)
        {
            AccountAddress = accountAddress;
            TargetAddress = targetAddress;
        }

        public override IValue PlainValue =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                { (Text)"type_id", (Text)TypeId },
                {
                    (Text)"values", Dictionary.Empty
                    .Add("a", AccountAddress.Serialize())
                    .Add("t", TargetAddress.Serialize())
                }
            });

        public override void LoadPlainValue(IValue plainValue)
        {
            var dictionary = (Dictionary)plainValue;
            var values = (Dictionary)dictionary["values"];
            AccountAddress = values["a"].ToAddress();
            TargetAddress = values["t"].ToAddress();
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;

            CheckPermission(context);

            var account = states.GetAccount(AccountAddress);

            // Remove state
            account = account.RemoveState(TargetAddress);
            return states.SetAccount(AccountAddress, account);
        }
    }
}
