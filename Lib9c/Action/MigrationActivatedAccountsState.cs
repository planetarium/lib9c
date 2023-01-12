using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Model;
using Lib9c.Model.State;
using Libplanet.Action;
using Serilog;

namespace Lib9c.Action
{
    [Serializable]
    [ActionType("migration_activated_accounts_state")]
    public class MigrationActivatedAccountsState : GameAction
    {
        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states.SetState(Addresses.ActivatedAccount, MarkChanged);
            }

            CheckPermission(context);

            Log.Debug($"Start {nameof(MigrationActivatedAccountsState)}");
            if (states.TryGetState(Addresses.ActivatedAccount, out Dictionary rawState))
            {
                var activatedAccountsState = new ActivatedAccountsState(rawState);
                var accounts = activatedAccountsState.Accounts;
                foreach (var agentAddress in accounts)
                {
                    var address = agentAddress.Derive(ActivationKey.DeriveKey);
                    if (states.GetState(address) is null)
                    {
                        states = states.SetState(address, true.Serialize());
                    }
                }
                Log.Debug($"Finish {nameof(MigrationActivatedAccountsState)}");
                return states;
            }

            throw new ActivatedAccountsDoesNotExistsException();
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
        }
    }
}
