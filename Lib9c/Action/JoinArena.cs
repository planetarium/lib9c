using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("join_arena")]
    public class JoinArena : GameAction
    {
        public Address avatarAddress;
        public List<Guid> costumeIds;
        public List<Guid> equipmentIds;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>()
            {
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["costume_ids"] = new List(costumeIds
                    .OrderBy(element => element).Select(e => e.Serialize())),
                ["equipment_ids"] = new List(equipmentIds
                    .OrderBy(element => element).Select(e => e.Serialize())),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            costumeIds = ((List)plainValue["costume_ids"]).Select(e => e.ToGuid()).ToList();
            equipmentIds = ((List)plainValue["equipment_ids"]).Select(e => e.ToGuid()).ToList();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var blockIndex = context.BlockIndex;
            var arenaAddress = Addresses.Arena;

            var arenaStateAddress = ArenaState.DeriveAddress(blockIndex);
            var arenaAvatarStateAddress = ArenaAvatarState.DeriveAddress(avatarAddress);
            if (context.Rehearsal)
            {
                return states.SetState(arenaStateAddress, MarkChanged)
                    .SetState(arenaAvatarStateAddress, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, context.Signer, arenaAddress);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            if (!states.TryGetAgentAvatarStatesV2(context.Signer, avatarAddress, out var agentState,
                    out var avatarState))
            {
                throw new FailedLoadStateException($"Aborted as the avatar state of the signer failed to load.");
            }

            avatarState.ValidEquipmentAndCostume(costumeIds, equipmentIds,
                states.GetSheet<ItemRequirementSheet>(),
                states.GetSheet<EquipmentItemRecipeSheet>(),
                states.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                states.GetSheet<EquipmentItemOptionSheet>(),
                blockIndex, addressesHex);

            var sheet = states.GetSheet<ArenaSheet>();
            if (!TryGetRow(sheet, blockIndex, out var row))
            {
                throw new SheetRowNotFoundException(nameof(ArenaSheet), blockIndex);
            }

            if (!row.TryGetRound(blockIndex, out var round))
            {
                throw new RoundDoesNotExistException($"{nameof(JoinArena)} : {blockIndex}");
            }

            if (round.EntranceFee > 0) // transfer
            {
                states = states.TransferAsset(context.Signer, arenaAddress,
                    states.GetGoldCurrency() * round.EntranceFee);
            }

            var arenaState = GetArenaState(states, arenaStateAddress, blockIndex);
            arenaState.Add(avatarAddress);

            var arenaAvatarState = GetArenaAvatarState(states, arenaAvatarStateAddress, avatarState);
            arenaAvatarState.UpdateCostumes(costumeIds);
            arenaAvatarState.UpdateEquipment(equipmentIds);

            return states.SetState(arenaStateAddress, arenaState.Serialize())
                .SetState(arenaAvatarStateAddress, arenaAvatarState.Serialize())
                .SetState(context.Signer, agentState.Serialize());
        }

        private static bool TryGetRow(ArenaSheet sheet, long blockIndex, out ArenaSheet.Row row)
        {
            row = sheet.Values.FirstOrDefault(x => x.IsIn(blockIndex));
            return row != null;
        }

        private static ArenaState GetArenaState(IAccountStateDelta states,
            Address arenaStateAddress, long blockIndex)
        {
            return states.TryGetState(arenaStateAddress, out List list)
                ? new ArenaState(list)
                : new ArenaState(blockIndex);
        }

        private static ArenaAvatarState GetArenaAvatarState(IAccountStateDelta states,
            Address arenaAvatarStateAddress, AvatarState avatarState)
        {
            return states.TryGetState(arenaAvatarStateAddress, out List list)
                ? new ArenaAvatarState(list)
                : new ArenaAvatarState(avatarState);
        }
    }
}
