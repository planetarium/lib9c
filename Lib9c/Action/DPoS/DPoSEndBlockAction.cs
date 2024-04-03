using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Action.DPoS.Control;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Model;
using Nekoyume.Module;

namespace Nekoyume.Action.DPoS
{
    /// <summary>
    /// A EndBlock action for DPoS that updates <see cref="ValidatorSet"/>.
    /// </summary>
    public sealed class DPoSEndBlockAction : ActionBase
    {
        /// <summary>
        /// Creates a new instance of <see cref="DPoSBeginBlockAction"/>.
        /// </summary>
        public DPoSEndBlockAction()
        {
        }

       /// <inheritdoc cref="IAction.PlainValue"/>
        public override IValue PlainValue => new Bencodex.Types.Boolean(true);

        /// <inheritdoc cref="IAction.LoadPlainValue(IValue)"/>
        public override void LoadPlainValue(IValue plainValue)
        {
            // Method intentionally left empty.
        }

        /// <inheritdoc cref="IAction.Execute(IActionContext)"/>
        public override IWorld Execute(IActionContext context)
        {
            var states = context.PreviousState;

            // Update ValidatorSet
            states = ValidatorSetCtrl.Update(states, context);
            ValidatorSet bondedSet;
            (states, bondedSet) = ValidatorSetCtrl.FetchBondedValidatorSet(states);
            foreach (var validator in bondedSet.Set)
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
