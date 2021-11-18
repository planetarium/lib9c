using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Action;
using MessagePack;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("create_pending_activation")]
    [MessagePackObject]
    public class CreatePendingActivation : ActionBase
    {
        [Key(0)]
        public PendingActivationState PendingActivation { get; private set; }

        [IgnoreMember]
        public override IValue PlainValue
            => new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>(
                        (Text)"pending_activation",
                        PendingActivation.Serialize()
                    ),
                }
            );

        public CreatePendingActivation()
        {
        }

        [SerializationConstructor]
        public CreatePendingActivation(PendingActivationState activationKey)
        {
            PendingActivation = activationKey;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            if (context.Rehearsal)
            {
                return context.PreviousStates.SetState(PendingActivation.address, MarkChanged);
            }

            CheckPermission(context);

            return context.PreviousStates.SetState(
                PendingActivation.address,
                PendingActivation.Serialize()
            );
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = ((Bencodex.Types.Dictionary) plainValue);
            PendingActivation = new PendingActivationState((Dictionary) asDict["pending_activation"]);
        }
    }
}
