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
        public List<Guid> costumes;
        public List<Guid> equipments;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>()
            {
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["costumes"] = new List(costumes
                    .OrderBy(element => element).Select(e => e.Serialize())),
                ["equipments"] = new List(equipments
                    .OrderBy(element => element).Select(e => e.Serialize())),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            costumes = ((List)plainValue["costumes"]).Select(e => e.ToGuid()).ToList();
            equipments = ((List)plainValue["equipments"]).Select(e => e.ToGuid()).ToList();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var currentBlockIndex = context.BlockIndex;
            var arenaAddress = Addresses.Arena;

            var arenaAvatarStateAddress = ArenaAvatarState.DeriveAddress(avatarAddress);
            if (context.Rehearsal)
            {
                return states.SetState(ArenaState.DeriveAddress(currentBlockIndex), MarkChanged)
                    .SetState(arenaAvatarStateAddress, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, context.Signer, arenaAddress);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            if (!states.TryGetAgentAvatarStatesV2(context.Signer, avatarAddress,
                    out var agentState, out var avatarState, out _))
            {
                throw new FailedLoadStateException(
                    $"Aborted as the avatar state of the signer failed to load.");
            }

            avatarState.ValidEquipmentAndCostume(costumes, equipments,
                states.GetSheet<ItemRequirementSheet>(),
                states.GetSheet<EquipmentItemRecipeSheet>(),
                states.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                states.GetSheet<EquipmentItemOptionSheet>(),
                currentBlockIndex, addressesHex);

            var sheet = states.GetSheet<ArenaSheet>();
            var row = sheet.Values.FirstOrDefault(x => x.IsIn(currentBlockIndex));
            if (row is null)
            {
                throw new SheetRowNotFoundException(nameof(ArenaSheet), currentBlockIndex);
            }

            if (!row.TryGetRound(currentBlockIndex, out var round))
            {
                throw new RoundDoesNotExistException($"{nameof(JoinArena)} : {currentBlockIndex}");
            }

            if (round.EntranceFee > 0)
            {
                // todo: 테스트용
                // states = states.TransferAsset(context.Signer, arenaAddress,
                //     states.GetGoldCurrency() * round.EntranceFee);
                var costCrystal = round.EntranceFee * CrystalCalculator.CRYSTAL;
                states = states.TransferAsset(context.Signer, arenaAddress, costCrystal);
            }

            var arenaStateAddress = ArenaState.DeriveAddress(round.StartBlockIndex);
            var arenaState = GetArenaState(states, arenaStateAddress, round.StartBlockIndex);
            arenaState.Add(avatarAddress);

            var arenaAvatarState = GetArenaAvatarState(states, arenaAvatarStateAddress, avatarState);
            if (arenaAvatarState.Records.IsExist(round.StartBlockIndex))
            {
                throw new AlreadyEnteredException($"{nameof(JoinArena)} : {round.StartBlockIndex}");
            }

            var totalWin = GetTotalWin(row, arenaAvatarState);
            if (totalWin < round.RequiredWins)
            {
                throw new NotEnoughWinException($"{addressesHex} {totalWin} < {round.RequiredWins}");
            }

            arenaAvatarState.Records.Add(round.StartBlockIndex);
            arenaAvatarState.UpdateCostumes(costumes);
            arenaAvatarState.UpdateEquipment(equipments);

            return states
                .SetState(arenaStateAddress, arenaState.Serialize())
                .SetState(arenaAvatarStateAddress, arenaAvatarState.Serialize())
                .SetState(context.Signer, agentState.Serialize());
        }

        private static int GetTotalWin(ArenaSheet.Row row, ArenaAvatarState arenaAvatarState)
        {
            var totalWin = 0;
            foreach (var roundData in row.Round)
            {
                if (arenaAvatarState.Records.TryGetRecord(roundData.StartBlockIndex, out var record))
                {
                    totalWin += record.Win;
                }
            }

            return totalWin;
        }

        private static ArenaState GetArenaState(IAccountStateDelta states,
            Address arenaStateAddress, long startBlockIndex)
        {
            return states.TryGetState(arenaStateAddress, out List list)
                ? new ArenaState(list)
                : new ArenaState(startBlockIndex);
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
