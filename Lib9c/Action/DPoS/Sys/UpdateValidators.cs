using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Action.DPoS.Control;
using Nekoyume.Action.DPoS.Model;
using Nekoyume.Module;

namespace Nekoyume.Action.DPoS.Sys
{
    /// <summary>
    /// An action for update validators.
    /// Should be executed at the end of the block.
    /// </summary>
    public sealed class UpdateValidators : ActionBase
    {
        /// <summary>
        /// Creates a new instance of <see cref="AllocateReward"/>.
        /// </summary>
        public UpdateValidators()
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
            states = ValidatorSetCtrl.Update(states, context);
            ValidatorSet bondedSet;
            (states, bondedSet) = ValidatorSetCtrl.FetchBondedValidatorSet(states);
            var validatorSet = new Libplanet.Types.Consensus.ValidatorSet(
                bondedSet.Set.Select(
                        v => new Libplanet.Types.Consensus.Validator(
                            v.OperatorPublicKey,
                            v.ConsensusToken.RawValue))
                    .ToList());
            states = states.SetValidatorSet(validatorSet);

            return states;
        }
    }
}
