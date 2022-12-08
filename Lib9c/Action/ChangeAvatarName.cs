using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("change_avatar_name")]
    public class ChangeAvatarName : GameAction
    {
        public Address TargetAvatarAddr;
        public string Name;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["l"] = new List<IValue>
                {
                    TargetAvatarAddr.Serialize(),
                    Name.Serialize()
                }.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            var list = (List)plainValue["l"];
            TargetAvatarAddr = list[0].ToAddress();
            Name = list[1].ToDotnetString();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            // Return the previous states when the context in rehearsal.
            if (context.Rehearsal)
            {
                return context.PreviousStates;
            }

            // Validate member fields.
            if (!Regex.IsMatch(Name, GameConfig.AvatarNickNamePattern))
            {
                throw new InvalidNamePatternException(
                    $"Aborted as the input name({Name}) does not follow the allowed name pattern.");
            }

            var states = context.PreviousStates;

            // Get the avatar state from the previous states.
            if (!states.TryGetAgentAvatarStatesV2(
                    context.Signer,
                    TargetAvatarAddr,
                    out var agentState,
                    out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException(
                    "Aborted as the avatar state of the signer was failed to load.");
            }

            // Set name.
            avatarState.name = Name;

            // Update the avatar state to the next states.
            return states.SetState(avatarState.address, avatarState.SerializeV2());
        }
    }
}
