#nullable enable
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c.Formatters;
using Lib9c.Renderers;
using Libplanet.Crypto;
using Libplanet.Common;
using Libplanet.Types.Tx;
using MessagePack;

namespace Nekoyume.Action
{
    [MessagePackObject]
    public struct NCActionEvaluation
    {
#pragma warning disable MsgPack003
        [Key(0)]

        [MessagePackFormatter(typeof(NCActionFormatter))]
        public ActionBase? Action { get; set; }

        [Key(1)]
        [MessagePackFormatter(typeof(AddressFormatter))]
        public Address Signer { get; set; }
#pragma warning restore MsgPack003

        [Key(2)]
        public long BlockIndex { get; set; }

        [Key(3)]
        [MessagePackFormatter(typeof(HashDigestFormatter))]
        public HashDigest<SHA256> OutputState { get; set; }

        [Key(4)]
        [MessagePackFormatter(typeof(ExceptionFormatter<Exception>))]
        public Exception? Exception { get; set; }

        [Key(5)]
        [MessagePackFormatter(typeof(HashDigestFormatter))]
        public HashDigest<SHA256> PreviousState { get; set; }

        [Key(6)]
        public int RandomSeed { get; set; }

        [Key(7)]
        public Dictionary<string, IValue> Extra { get; set; }

        [Key(8)]
        [MessagePackFormatter(typeof(TxIdFormatter))]
#pragma warning disable MsgPack003
        public TxId? TxId { get; set; }
#pragma warning restore MsgPack003

        [SerializationConstructor]
        public NCActionEvaluation(
            ActionBase? action,
            Address signer,
            long blockIndex,
            HashDigest<SHA256> outputStates,
            Exception? exception,
            HashDigest<SHA256> previousStates,
            int randomSeed,
            Dictionary<string, IValue> extra,
            TxId? txId
        )
        {
            Action = action;
            Signer = signer;
            BlockIndex = blockIndex;
            OutputState = outputStates;
            Exception = exception;
            PreviousState = previousStates;
            RandomSeed = randomSeed;
            Extra = extra;
            TxId = txId;
        }

        /// <summary>
        /// Converts this network DTO to <see cref="ActionEvaluation{T}"/> for render pipeline.
        /// </summary>
        /// <remarks>
        /// On iOS (IL2CPP), under certain conditions (e.g., specific stripping/optimization combinations),
        /// the object-initializer path like
        /// <c>new ActionEvaluation&lt;ActionBase&gt; { Action = ..., ... }</c>
        /// was observed to produce incorrect code, leaving <c>ActionEvaluation&lt;ActionBase&gt;.Action</c> null
        /// even when the source <see cref="Action"/> was non-null.
        ///
        /// To avoid that IL2CPP/AOT edge case, this conversion intentionally uses step-by-step assignments
        /// to follow a more stable codegen path. (No behavioral change intended; stability/reproducibility only.)
        /// </remarks>
        public ActionEvaluation<ActionBase> ToActionEvaluation()
        {
            var eval = new ActionEvaluation<ActionBase>();
            var actionValue = Action ?? new RewardGold();

            eval.Action = actionValue;
            eval.Signer = Signer;
            eval.BlockIndex = BlockIndex;
            eval.OutputState = OutputState;
            eval.Exception = Exception;
            eval.PreviousState = PreviousState;
            eval.RandomSeed = RandomSeed;
            eval.Extra = Extra;
            eval.TxId = TxId;

            return eval;
        }
    }
}
