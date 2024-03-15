using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.DPoS.Control;
using Lib9c.DPoS.Misc;
using Lib9c.DPoS.Model;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Module;

namespace Lib9c.DPoS.Action
{
    /// <summary>
    /// A block action for DPoS that updates <see cref="ValidatorSet"/>.
    /// </summary>
    public sealed class PoSAction : IAction
    {
        /// <summary>
        /// Creates a new instance of <see cref="PoSAction"/>.
        /// </summary>
        public PoSAction()
        {
        }

       /// <inheritdoc cref="IAction.PlainValue"/>
        public IValue PlainValue => new Bencodex.Types.Boolean(true);

        /// <inheritdoc cref="IAction.LoadPlainValue(IValue)"/>
        public void LoadPlainValue(IValue plainValue)
        {
            // Method intentionally left empty.
        }

        /// <inheritdoc cref="IAction.Execute(IActionContext)"/>
        public IWorld Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousState;

            // if (ctx.Rehearsal)
            // Rehearsal mode is not implemented
            states = ValidatorSetCtrl.Update(states, ctx);
            var nativeTokens = ImmutableHashSet.Create(
                Asset.GovernanceToken, Asset.ConsensusToken, Asset.Share);

            states = AllocateReward.Execute(
                states,
                ctx,
                nativeTokens,
                ctx.LastCommit?.Votes,
                ctx.Miner);

            // Endblock, Update ValidatorSet
            var bondedSet = ValidatorSetCtrl.FetchBondedValidatorSet(states).Item2.Set;
            foreach (var validator in bondedSet)
            {
                states = states.SetValidator(
                    new Libplanet.Types.Consensus.Validator(
                        validator.OperatorPublicKey,
                        validator.ConsensusToken.RawValue));
            }

            return states;
        }
    }
}
