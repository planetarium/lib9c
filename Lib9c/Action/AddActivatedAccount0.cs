using System;
using System.Collections.Generic;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.State;
using Nekoyume.Module;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V200020AccidentObsoleteIndex)]
    [ActionType("add_activated_account")]
    public class AddActivatedAccount0 : ActionBase, IAddActivatedAccountV1
    {
        public AddActivatedAccount0(Address address)
        {
            Address = address;
        }

        public AddActivatedAccount0()
        {
        }

        public Address Address { get; private set; }

        Address IAddActivatedAccountV1.Address => Address;

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", "add_activated_account")
            .Add("values", new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>((Text)"address", Address.Serialize()),
                }
            ));

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            IWorld state = context.PreviousState;

            if (!state.TryGetLegacyState(ActivatedAccountsState.Address, out Dictionary accountsAsDict))
            {
                throw new ActivatedAccountsDoesNotExistsException();
            }

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);
            CheckPermission(context);

            var accounts = new ActivatedAccountsState(accountsAsDict);
            return state.SetLegacyState(
                ActivatedAccountsState.Address,
                accounts.AddAccount(Address).Serialize()
            );
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary)((Dictionary)plainValue)["values"];
            Address = asDict["address"].ToAddress();
        }
    }
}
