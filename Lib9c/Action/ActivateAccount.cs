using System;
using System.Collections.Generic;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("activate_account2")]
    [ActionObsolete(ActionObsoleteConfig.V200030ObsoleteIndex)]
    public class ActivateAccount : ActionBase, IActivateAccount
    {
        public Address PendingAddress { get; private set; }

        public byte[] Signature { get; private set; }

        Address IActivateAccount.PendingAddress => PendingAddress;
        byte[] IActivateAccount.Signature => Signature;

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", "activate_account2")
            .Add("values", new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>((Text)"pa", PendingAddress.Serialize()),
                    new KeyValuePair<IKey, IValue>((Text)"s", (Binary) Signature),
                }
            ));

        public ActivateAccount()
        {
        }

        public ActivateAccount(Address pendingAddress, byte[] signature)
        {
            PendingAddress = pendingAddress;
            Signature = signature;
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            IWorld state = context.PreviousState;
            Address activatedAddress = context.Signer.Derive(ActivationKey.DeriveKey);

            CheckObsolete(ActionObsoleteConfig.V200030ObsoleteIndex, context);

            if (!(state.GetLegacyState(activatedAddress) is null))
            {
                throw new AlreadyActivatedException($"{context.Signer} already activated.");
            }
            if (!state.TryGetLegacyState(PendingAddress, out Dictionary pendingAsDict))
            {
                throw new PendingActivationDoesNotExistsException(PendingAddress);
            }

            var pending = new PendingActivationState(pendingAsDict);

            if (pending.Verify(this))
            {
                // We left this log message to track activation history.
                // Please delete it if we have an API for evaluation results on the Libplanet side.
                Log.Information("{pendingAddress} is activated by {signer} now.", pending.address, context.Signer);
                return state
                    .SetLegacyState(activatedAddress, true.Serialize())
                    .SetLegacyState(pending.address, new Bencodex.Types.Null());
            }
            else
            {
                throw new InvalidSignatureException(pending, Signature);
            }
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary)((Dictionary)plainValue)["values"];
            PendingAddress = asDict["pa"].ToAddress();
            Signature = ((Binary)asDict["s"]).ToByteArray();
        }

        public Address GetPendingAddress() => PendingAddress;

        public byte[] GetSignature() => Signature;
    }
}
