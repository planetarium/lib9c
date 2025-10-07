using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Action;
using Lib9c.Model;
using Lib9c.Model.State;
using Lib9c.Module;
using Lib9c.TableData.WorldAndStage;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.DevExtensions.Action.Stage
{
    [Serializable]
    [ActionType("clear_stage")]
    public class ClearStage : GameAction
    {
        public Address AvatarAddress { get; set; }
        public int TargetStage { get; set; }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            var avatarState = states.GetAvatarState(AvatarAddress);
            if (avatarState is null || !avatarState.agentAddress.Equals(context.Signer))
            {
                var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            avatarState.worldInformation = new WorldInformation(
                context.BlockIndex,
                states.GetSheet<WorldSheet>(),
                TargetStage
            );
            return states.SetAvatarState(AvatarAddress, avatarState);
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = AvatarAddress.Serialize(),
                ["targetStage"] = TargetStage.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue
        )
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            TargetStage = plainValue["targetStage"].ToInteger();
        }
    }
}
