using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bencodex.Types;
using JetBrains.Annotations;
using Libplanet;
using Libplanet.Action;
using MessagePack;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace Nekoyume.Action
{
    [MessagePackObject]
    public struct NCActionEvaluation
    {
#pragma warning disable MsgPack003
        [Key(0)] [CanBeNull] public NCAction Action { get; set; }

        [Key(1)]
        public Address Signer { get; set; }
#pragma warning restore MsgPack003

        [Key(2)]
        public long BlockIndex { get; set; }

        [Key(3)]
        public IAccountStateDelta OutputStates { get; set; }

        [Key(4)]
        public Exception Exception { get; set; }

        [Key(5)]
        public IAccountStateDelta PreviousStates { get; set; }

        [Key(6)]
        public int RandomSeed { get; set; }

        [Key(7)]
        public Dictionary<string, IValue> Extra { get; set; }


        [SerializationConstructor]
        public NCActionEvaluation(
            NCAction action,
            Address signer,
            long blockIndex,
            IAccountStateDelta outputStates,
            Exception exception,
            IAccountStateDelta previousStates,
            int randomSeed,
            Dictionary<string, IValue> extra
        )
        {
            Action = action;
            Signer = signer;
            BlockIndex = blockIndex;
            OutputStates = outputStates;
            Exception = exception;
            PreviousStates = previousStates;
            RandomSeed = randomSeed;
            Extra = extra;
        }

        public ActionBase.ActionEvaluation<IAction> ToActionEvaluation()
        {
            return new ActionBase.ActionEvaluation<IAction>
            {
                Action =  Action is null ? new RewardGold() : Action.InnerAction,
                Signer = Signer,
                BlockIndex = BlockIndex,
                OutputStates = OutputStates,
                Exception = Exception,
                PreviousStates = PreviousStates,
                RandomSeed = RandomSeed,
                Extra = Extra
            };
        }
    }
}
