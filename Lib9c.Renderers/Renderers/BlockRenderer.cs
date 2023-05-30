using System;
using Libplanet.Action;
using Libplanet.Blockchain.Renderers;
using Libplanet.Blocks;
using Nekoyume.Action;
#if UNITY_EDITOR || UNITY_STANDALONE
using UniRx;
#else
using System.Reactive.Subjects;
using System.Reactive.Linq;
#endif

namespace Lib9c.Renderers
{
    using NCAction = PolymorphicAction<ActionBase>;
    using NCBlock = Block;

    public class BlockRenderer : IRenderer
    {
        public readonly Subject<(NCBlock OldTip, NCBlock NewTip)> BlockSubject =
            new Subject<(NCBlock OldTip, NCBlock NewTip)>();

        public readonly Subject<(NCBlock OldTip, NCBlock NewTip, NCBlock Branchpoint)> ReorgSubject =
            new Subject<(NCBlock OldTip, NCBlock NewTip, NCBlock Branchpoint)>();

        public readonly Subject<(NCBlock OldTip, NCBlock NewTip, NCBlock Branchpoint)> ReorgEndSubject =
            new Subject<(NCBlock OldTip, NCBlock NewTip, NCBlock Branchpoint)>();

        public void RenderBlock(NCBlock oldTip, NCBlock newTip) =>
            BlockSubject.OnNext((oldTip, newTip));
    }
}
