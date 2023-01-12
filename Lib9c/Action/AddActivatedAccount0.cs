using System;
using System.Collections.Generic;
using Bencodex.Types;
using Lib9c.Model.State;
using Lib9c.Policy;
using Libplanet;
using Libplanet.Action;

namespace Lib9c.Action
{
    [Serializable]
    [ActionObsolete(BlockPolicySource.V100080ObsoleteIndex)]
    [ActionType("add_activated_account")]
    public class AddActivatedAccount0 : ActionBase
    {
        public AddActivatedAccount0(Address address)
        {
            Address = address;
        }

        public AddActivatedAccount0()
        {
        }

        public Address Address { get; private set; }

        public override IValue PlainValue =>
            new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>((Text)"address", Address.Serialize()),
                }
            );

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta state = context.PreviousStates;

            if (context.Rehearsal)
            {
                return state
                    .SetState(ActivatedAccountsState.Address, MarkChanged);
            }

            if (!state.TryGetState(ActivatedAccountsState.Address, out Dictionary accountsAsDict))
            {
                throw new ActivatedAccountsDoesNotExistsException();
            }

            CheckObsolete(BlockPolicySource.V100080ObsoleteIndex, context);
            CheckPermission(context);

            var accounts = new ActivatedAccountsState(accountsAsDict);
            return state.SetState(
                ActivatedAccountsState.Address,
                accounts.AddAccount(Address).Serialize()
            );
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary) plainValue;
            Address = asDict["address"].ToAddress();
        }
    }
}
