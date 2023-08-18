using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/430
    /// Updated at https://github.com/planetarium/lib9c/pull/474
    /// Updated at https://github.com/planetarium/lib9c/pull/602
    /// Updated at https://github.com/planetarium/lib9c/pull/861
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionType("charge_action_point3")]
    public class ChargeActionPoint : GameAction, IChargeActionPointV1
    {
        public Address avatarAddress;

        Address IChargeActionPointV1.AvatarAddress => avatarAddress;

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var world = context.PreviousState;
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);

            if (context.Rehearsal)
            {
                world = LegacyModule.SetState(world, inventoryAddress, MarkChanged);
                world = LegacyModule.SetState(world, worldInformationAddress, MarkChanged);
                world = LegacyModule.SetState(world, questListAddress, MarkChanged);
                world = LegacyModule.SetState(world, avatarAddress, MarkChanged);
                return world;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}ChargeActionPoint exec started", addressesHex);

            if (!AvatarModule.TryGetAvatarStateV2(
                    world,
                    context.Signer,
                    avatarAddress,
                    out var avatarState,
                    out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var row = LegacyModule.GetSheet<MaterialItemSheet>(world)
                .Values.First(r => r.ItemSubType == ItemSubType.ApStone);
            if (!avatarState.inventory.RemoveFungibleItem(row.ItemId, context.BlockIndex))
            {
                throw new NotEnoughMaterialException(
                    $"{addressesHex}Aborted as the player has no enough material ({row.Id})");
            }

            var gameConfigState = LegacyModule.GetGameConfigState(world);
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the game config state was failed to load.");
            }

            if (avatarState.actionPoint == gameConfigState.ActionPointMax)
            {
                throw new ActionPointExceededException();
            }

            avatarState.actionPoint = gameConfigState.ActionPointMax;
            world = LegacyModule.SetState(world, inventoryAddress, avatarState.inventory.Serialize());
            world = LegacyModule.SetState(
                world,
                worldInformationAddress,
                avatarState.worldInformation.Serialize());
            world = LegacyModule.SetState(
                world,
                questListAddress,
                avatarState.questList.Serialize());
            world = AvatarModule.SetAvatarStateV2(world, avatarAddress, avatarState);
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}ChargeActionPoint Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return world;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = avatarAddress.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
        }
    }
}
