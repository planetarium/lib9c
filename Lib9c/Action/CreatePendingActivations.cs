using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("create_pending_activations")]
    public class CreatePendingActivations : ActionBase
    {
        public IList<(byte[] Address, byte[] Nonce, byte[] PublicKey)> PendingActivations { get; internal set; }

        public override IValue PlainValue
            => PendingActivations.Select(t => new List(new Binary[] { t.Address, t.Nonce, t.PublicKey }.Cast<IValue>()))
            .Serialize();

        public CreatePendingActivations()
        {
        }

        public CreatePendingActivations(IEnumerable<PendingActivationState> states)
        {
            PendingActivations = states.Select(pa => (pa.address.ToByteArray(), pa.Nonce, pa.PublicKey.Format(true))).ToImmutableList();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            CheckPermission(context);
            var state = context.PreviousStates;
            foreach ((byte[] address, byte[] nonce, byte[] publicKey) in PendingActivations)
            {
                state = state.SetState(
                    new Address(address),
                    new Dictionary(
                        new[]
                        {
                            new KeyValuePair<IKey, IValue>((Text)"address", (Binary)address),
                            new KeyValuePair<IKey, IValue>((Text)"nonce", (Binary)nonce),
                            new KeyValuePair<IKey, IValue>((Text)"public_key", (Binary)publicKey),
                        }
                    )
                );
            }

            return state;
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            PendingActivations = ((List)plainValue)
                .Cast<List>()
                .Select(v => (((Binary)v[0]).ToByteArray(), ((Binary)v[1]).ToByteArray(), ((Binary)v[2]).ToByteArray()))
                .ToImmutableList();
        }
    }
}
