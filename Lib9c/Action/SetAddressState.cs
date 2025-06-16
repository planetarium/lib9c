using System;
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
    public class SetAddressState : ActionBase
    {
        public const string TypeId = "set_address_state";

        public IReadOnlyList<(Address accountAddress, Address targetAddress, IValue state)> States { get; private set; }

        public SetAddressState()
        {
        }

        public SetAddressState(IReadOnlyList<(Address accountAddress, Address targetAddress, IValue state)> states)
        {
            if (states == null || states.Any(s => s.state == null))
            {
                throw new ArgumentNullException(nameof(states));
            }

            States = states;
        }

        public override IValue PlainValue =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                { (Text)"type_id", (Text)TypeId },
                {
                    (Text)"values", Dictionary.Empty
                    .Add("s", new List(States.Select(s =>
                        List.Empty
                            .Add(s.accountAddress.Serialize())
                            .Add(s.targetAddress.Serialize())
                            .Add(s.state))))
                }
            });

        public override void LoadPlainValue(IValue plainValue)
        {
            var dictionary = (Dictionary)plainValue;
            var values = (Dictionary)dictionary["values"];
            var states = ((List)values["s"])
                .Select(s =>
                {
                    var list = (List)s;
                    var state = list[2];
                    if (state is null || state.Equals(Null.Value))
                    {
                        throw new ArgumentNullException(nameof(state));
                    }
                    return (list[0].ToAddress(), list[1].ToAddress(), state);
                })
                .ToList();
            States = states;
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            CheckPermission(context);

            foreach (var (accountAddress, targetAddress, state) in States)
            {
                var account = states.GetAccount(accountAddress);
                var existingState = account.GetState(targetAddress);
                if (existingState is not null)
                {
                    throw new InvalidOperationException($"State already exists at address {targetAddress}");
                }

                account = account.SetState(targetAddress, state);
                states = states.SetAccount(accountAddress, account);
            }
            return states;
        }
    }
}
