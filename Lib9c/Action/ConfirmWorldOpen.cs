using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    /// <summary>
    /// Confirms a hard world opening whose conditions are already met. Anyone may sign it; it
    /// exists so that an opening whose threshold was lowered by a table patch can be confirmed
    /// without waiting for the next progress tick. It never fails: when the opening is unknown,
    /// already confirmed, past its fallback, not yet satisfied, or the sheets are absent, the
    /// previous state is returned unchanged.
    /// </summary>
    [ActionType("confirm_world_open")]
    public class ConfirmWorldOpen : GameAction
    {
        /// <summary>
        /// The <see cref="WorldOpenSheet"/> row id to confirm.
        /// </summary>
        public int OpenId;

        /// <inheritdoc />
        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            if (!states.TryGetPatchedSheet<WorldOpenSheet>(out var opens)
                || !states.TryGetPatchedSheet<WorldOpenConditionSheet>(out var conditions)
                || !opens.TryGetValue(OpenId, out var open)
                || !states.IsPending(open, context.BlockIndex)
                || !WorldOpenCounter.IsSatisfied(states, conditions, OpenId))
            {
                return states;
            }

            return states.ConfirmOpen(OpenId, context.BlockIndex);
        }

        /// <inheritdoc />
        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty.Add("o", (Integer)OpenId);

        /// <inheritdoc />
        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            OpenId = (Integer)plainValue["o"];
        }
    }
}
