using System;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain.Renderers;
using Libplanet.Blocks;
using Nekoyume.Action;
using static Nekoyume.Action.ActionBase;
using Serilog;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
using System.Reactive.Subjects;
using System.Reactive.Linq;
#endif

namespace Lib9c.Renderer
{
    using NCAction = PolymorphicAction<ActionBase>;
    using NCBlock = Block<PolymorphicAction<ActionBase>>;

    public class ActionRenderer : IActionRenderer<NCAction>
    {
        public Subject<ActionEvaluation<IAction>> ActionRenderSubject { get; }
            = new Subject<ActionEvaluation<IAction>>();

        public Subject<ActionEvaluation<IAction>> ActionUnrenderSubject { get; }
            = new Subject<ActionEvaluation<IAction>>();

        public readonly Subject<(NCBlock OldTip, NCBlock NewTip)> BlockEndSubject =
            new Subject<(NCBlock OldTip, NCBlock NewTip)>();

        public void RenderAction(IAction action, IActionContext context, IAccountStateDelta nextStates) =>
            ActionRenderSubject.OnNext(new ActionEvaluation<IAction>
            {
                Action = GetActionBase(action),
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                OutputStates = nextStates,
                PreviousStates = context.PreviousStates,
                RandomSeed = context.Random.Seed
            });

        public void UnrenderAction(IAction action, IActionContext context, IAccountStateDelta nextStates) =>
            ActionUnrenderSubject.OnNext(new ActionEvaluation<IAction>
            {
                Action = GetActionBase(action),
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                OutputStates = nextStates,
                PreviousStates = context.PreviousStates,
                RandomSeed = context.Random.Seed
            });

        public void RenderActionError(
            IAction action,
            IActionContext context,
            Exception exception
        )
        {
            Log.Error(exception, "{action} execution failed.", action);
            ActionRenderSubject.OnNext(new ActionEvaluation<IAction>
            {
                Action = GetActionBase(action),
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                OutputStates = context.PreviousStates,
                Exception = exception,
                PreviousStates = context.PreviousStates,
                RandomSeed = context.Random.Seed
            });
        }

        public void UnrenderActionError(IAction action, IActionContext context, Exception exception) =>
            ActionUnrenderSubject.OnNext(new ActionEvaluation<IAction>
            {
                Action = GetActionBase(action),
                Signer = context.Signer,
                BlockIndex = context.BlockIndex,
                OutputStates = context.PreviousStates,
                Exception = exception,
                PreviousStates = context.PreviousStates,
                RandomSeed = context.Random.Seed
            });

        [Obsolete("Use BlockRenderer.RenderBlock(oldTip, newTip)")]
        public void RenderBlock(NCBlock oldTip, NCBlock newTip)
        {
            // RenderBlock should be handled by BlockRenderer
        }

        public void RenderBlockEnd(NCBlock oldTip, NCBlock newTip)
        {
            BlockEndSubject.OnNext((oldTip, newTip));
        }

        [Obsolete("Use BlockRenderer.RenderReorg(oldTip, newTip, branchpoint)")]
        public void RenderReorg(NCBlock oldTip, NCBlock newTip, NCBlock branchpoint)
        {
            // RenderReorg should be handled by BlockRenderer
        }

        [Obsolete("Use BlockRenderer.RenderReorgEnd(oldTip, newTip, branchpoint)")]
        public void RenderReorgEnd(NCBlock oldTip, NCBlock newTip, NCBlock branchpoint)
        {
            // RenderReorgEnd should be handled by BlockRenderer
        }

        public IObservable<ActionEvaluation<T>> EveryRender<T>() =>
            ActionRenderSubject
                .AsObservable()
                .Where(eval => eval.Action is T)
                .Select(eval => new ActionEvaluation<T>
                {
                    Action = (T)eval.Action,
                    Signer = eval.Signer,
                    BlockIndex = eval.BlockIndex,
                    OutputStates = eval.OutputStates,
                    Exception = eval.Exception,
                    PreviousStates = eval.PreviousStates,
                    RandomSeed = eval.RandomSeed,
                    Extra = eval.Extra,
                });

        public IObservable<ActionEvaluation<T>> EveryUnrender<T>() where T : ActionBase =>
            ActionUnrenderSubject
                .AsObservable()
                .Where(eval => eval.Action is T)
                .Select(eval => new ActionEvaluation<T>
                {
                    Action = (T)eval.Action,
                    Signer = eval.Signer,
                    BlockIndex = eval.BlockIndex,
                    OutputStates = eval.OutputStates,
                    Exception = eval.Exception,
                    PreviousStates = eval.PreviousStates,
                    RandomSeed = eval.RandomSeed
                });

        public IObservable<ActionEvaluation<ActionBase>> EveryRender(Address updatedAddress) =>
            ActionRenderSubject
                .AsObservable()
                .Where(eval => eval.OutputStates.UpdatedAddresses.Contains(updatedAddress))
                .Select(eval => new ActionEvaluation<ActionBase>
                {
                    Action = (ActionBase) eval.Action,
                    Signer = eval.Signer,
                    BlockIndex = eval.BlockIndex,
                    OutputStates = eval.OutputStates,
                    Exception = eval.Exception,
                    PreviousStates = eval.PreviousStates,
                    RandomSeed = eval.RandomSeed,
                    Extra = eval.Extra,
                });

        public IObservable<ActionEvaluation<IAction>> EveryUnrender(Address updatedAddress) =>
            ActionUnrenderSubject
                .AsObservable()
                .Where(eval => eval.OutputStates.UpdatedAddresses.Contains(updatedAddress))
                .Select(eval => new ActionEvaluation<IAction>
                {
                    Action = eval.Action,
                    Signer = eval.Signer,
                    BlockIndex = eval.BlockIndex,
                    OutputStates = eval.OutputStates,
                    Exception = eval.Exception,
                    PreviousStates = eval.PreviousStates,
                    RandomSeed = eval.RandomSeed,
                    Extra = eval.Extra,
                });

        private static ActionBase GetActionBase(IAction action)
        {
            if (action is NCAction polymorphicAction)
            {
                return polymorphicAction.InnerAction;
            }

            return (ActionBase)action;
        }
    }
}
