using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Lib9c.Model.Item;
using Lib9c.Model.Mail;
using Lib9c.Model.State;
using Lib9c.Module;
using Lib9c.TableData.Item;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;

namespace Lib9c.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V200020AccidentObsoleteIndex)]
    [ActionType("daily_reward2")]
    public class DailyReward2 : GameAction, IDailyRewardV1
    {
        public Address avatarAddress;
        public DailyRewardResult dailyRewardResult;
        private const int rewardItemId = 400000;
        private const int rewardItemCount = 10;

        Address IDailyRewardV1.AvatarAddress => avatarAddress;

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            IActionContext ctx = context;
            var states = ctx.PreviousState;

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            if (states.GetAgentState(context.Signer) is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the agent state of the signer was failed to load.");
            }

            if (!states.TryGetAvatarState(ctx.Signer, avatarAddress, out AvatarState avatarState))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the game config was failed to load.");
            }

            if (ctx.BlockIndex - avatarState.dailyRewardReceivedIndex >= gameConfigState.DailyRewardInterval)
            {
                avatarState.dailyRewardReceivedIndex = ctx.BlockIndex;
                avatarState.actionPoint = gameConfigState.ActionPointMax;
            }

            // create item
            var materialSheet = states.GetSheet<MaterialItemSheet>();
            var materials = new Dictionary<Material, int>();
            var material = ItemFactory.CreateMaterial(materialSheet, rewardItemId);
            materials[material] = rewardItemCount;

            var result = new DailyRewardResult
            {
                materials = materials,
            };

            // create mail
            var random = ctx.GetRandom();
            var mail = new DailyRewardMail(result,
                                           ctx.BlockIndex,
                                           random.GenerateRandomGuid(),
                                           ctx.BlockIndex);

            result.id = mail.id;
            dailyRewardResult = result;
            avatarState.Update(mail);
            avatarState.UpdateFromAddItem(material, rewardItemCount, false);
            return states.SetAvatarState(avatarAddress, avatarState);
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["avatarAddress"] = avatarAddress.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
        }


        [Serializable]
        public class DailyRewardResult : AttachmentActionResult
        {
            public Dictionary<Material, int> materials;
            public Guid id;

            protected override string TypeId => "dailyReward.dailyRewardResult";

            public DailyRewardResult()
            {
            }

            public DailyRewardResult(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                materials = serialized["materials"].ToDictionary_Material_int();
                id = serialized["id"].ToGuid();
            }

            public override IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "materials"] = materials.Serialize(),
                    [(Text) "id"] = id.Serialize(),
                }.Union((Bencodex.Types.Dictionary) base.Serialize()));
#pragma warning restore LAA1002
        }
    }
}
