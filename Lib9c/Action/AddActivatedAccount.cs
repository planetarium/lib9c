using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using MessagePack;
using Nekoyume.Model;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("add_activated_account2")]
    [MessagePackObject]
    public class AddActivatedAccount : ActionBase
    {
        public AddActivatedAccount(Address address)
        {
            Address = address;
        }

        public AddActivatedAccount()
        {
        }

        [Key(0)]
#pragma warning disable MsgPack003
        public Address Address { get; private set; }
#pragma warning restore MsgPack003

        [IgnoreMember]
        public override IValue PlainValue =>
            new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>((Text)"a", Address.Serialize()),
                }
            );

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta state = context.PreviousStates;
            var address = Address.Derive(ActivationKey.DeriveKey);

            if (context.Rehearsal)
            {
                return state
                    .SetState(address, MarkChanged);
            }

            if (!(state.GetState(address) is null))
            {
                throw new AlreadyActivatedException($"{address} is already activated.");
            }

            CheckPermission(context);

            return state.SetState(address, true.Serialize());
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary) plainValue;
            Address = asDict["a"].ToAddress();
        }
    }
}
