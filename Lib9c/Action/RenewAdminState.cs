using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/805
    /// Updated at https://github.com/planetarium/lib9c/pull/815
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionType("renew_admin_state")]
    public class RenewAdminState : GameAction, IRenewAdminStateV1
    {
        private const string NewValidUntilKey = "new_valid_until";
        public long NewValidUntil {get; internal set; }

        long IRenewAdminStateV1.NewValidUntil => NewValidUntil;

        public RenewAdminState()
        {
        }

        public RenewAdminState(long newValidUntil)
        {
            NewValidUntil = newValidUntil;
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var world = context.PreviousState;
            var account = world.GetAccount(ReservedAddresses.LegacyAccount);
            if (context.Rehearsal)
            {
                account = account
                    .SetState(Addresses.Admin, MarkChanged);
                return world.SetAccount(account);
            }

            if (TryGetAdminState(context, out AdminState adminState))
            {
                if (context.Signer != adminState.AdminAddress)
                {
                    throw new PermissionDeniedException(adminState, context.Signer);
                }

                var newAdminState = new AdminState(adminState.AdminAddress, NewValidUntil);
                account = account.SetState(Addresses.Admin,
                    newAdminState.Serialize());
            }

            return world.SetAccount(account);
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [NewValidUntilKey] = (Integer)NewValidUntil,
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            NewValidUntil = (Integer)plainValue[NewValidUntilKey];
        }
    }
}
