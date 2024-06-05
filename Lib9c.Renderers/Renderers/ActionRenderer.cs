using System;
using System.Security.Cryptography;
using Libplanet.Action;
using Libplanet.Action.Loader;
using Libplanet.Blockchain.Renderers;
using Libplanet.Types.Blocks;
using Nekoyume.Action;
using Serilog;
using Bencodex.Types;
using Libplanet.Common;
using Nekoyume.Action.Loader;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
using System.Reactive.Subjects;
using System.Reactive.Linq;
#endif

namespace Lib9c.Renderers
{
    public class ActionRenderer : IActionRenderer
    {
        private readonly IActionLoader _actionLoader;

        public Subject<ActionEvaluation<ActionBase>> ActionRenderSubject { get; }
            = new Subject<ActionEvaluation<ActionBase>>();

        public readonly Subject<(Block OldTip, Block NewTip)> BlockEndSubject =
            new Subject<(Block OldTip, Block NewTip)>();

        public ActionRenderer()
        {
            _actionLoader = new NCActionLoader();
        }

        public void RenderAction(IValue action, ICommittedActionContext context, HashDigest<SHA256> nextState) =>
            ActionRenderSubject.OnNext(new ActionEvaluation<ActionBase>
            {
                Action = context.IsBlockAction
                    ? new RewardGold()
                    : (ActionBase)_actionLoader.LoadAction(context.BlockIndex, action),
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                TxId = context.TxId,
                OutputState = nextState,
                PreviousState = context.PreviousState,
                RandomSeed = context.RandomSeed
            });

        public void RenderActionError(IValue action, ICommittedActionContext context, Exception exception)
        {
            Log.Error(exception, "{action} execution failed.", action);
            ActionRenderSubject.OnNext(new ActionEvaluation<ActionBase>
            {
                Action = context.IsBlockAction
                    ? new RewardGold()
                    : (ActionBase)_actionLoader.LoadAction(context.BlockIndex, action),
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                TxId = context.TxId,
                OutputState = context.PreviousState,
                Exception = exception,
                PreviousState = context.PreviousState,
                RandomSeed = context.RandomSeed
            });
        }

        [Obsolete("Use BlockRenderer.RenderBlock(oldTip, newTip)")]
        public void RenderBlock(Block oldTip, Block newTip)
        {
            // RenderBlock should be handled by BlockRenderer
        }

        public void RenderBlockEnd(Block oldTip, Block newTip)
        {
            BlockEndSubject.OnNext((oldTip, newTip));
        }

        public IObservable<ActionEvaluation<T>> EveryRender<T>() where T : ActionBase =>
            ActionRenderSubject
                .AsObservable()
                .Where(eval => eval.Action is T)
                .Select(eval => new ActionEvaluation<T>
                {
                    Action = (T)eval.Action,
                    Signer = eval.Signer,
                    BlockIndex = eval.BlockIndex,
                    TxId = eval.TxId,
                    OutputState = eval.OutputState,
                    Exception = eval.Exception,
                    PreviousState = eval.PreviousState,
                    RandomSeed = eval.RandomSeed,
                    Extra = eval.Extra,
                });
    }
}
