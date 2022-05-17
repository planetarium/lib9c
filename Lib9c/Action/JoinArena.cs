using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("join_arena")]
    public class JoinArena : GameAction
    {
        public Address DeriveArenaAddress(ArenaSheet.RoundData data)
        {
            return Addresses.Arena.Derive($"_{data.Id}_{data.Round}");
        }

        public Address avatarAddress;
        public int championshipId;
        public int round;
        public List<Guid> costumes;
        public List<Guid> equipments;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>()
            {
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["championshipId"] = championshipId.Serialize(),
                ["round"] = round.Serialize(),
                ["costumes"] = new List(costumes
                    .OrderBy(element => element).Select(e => e.Serialize())),
                ["equipments"] = new List(equipments
                    .OrderBy(element => element).Select(e => e.Serialize())),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            championshipId = plainValue["championshipId"].ToInteger();
            round = plainValue["round"].ToInteger();
            costumes = ((List)plainValue["costumes"]).Select(e => e.ToGuid()).ToList();
            equipments = ((List)plainValue["equipments"]).Select(e => e.ToGuid()).ToList();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
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
                context.BlockIndex, addressesHex);

            var sheet = states.GetSheet<ArenaSheet>();
            if (!sheet.TryGetValue(championshipId, out var row))
            {
                throw new SheetRowNotFoundException(
                    nameof(ArenaSheet), $"championship Id : {championshipId}");
            }

            if (!row.TryGetRound(context.BlockIndex, championshipId, round, out var roundData))
            {
                throw new RoundDoesNotExistException(
                    $"{nameof(JoinArena)} : championship Id({championshipId}) / round({round}) ");
            }

            // check fee
            if (roundData.EntranceFee > 0)
            {
                var costCrystal = roundData.EntranceFee * CrystalCalculator.CRYSTAL;
                var arenaAdr = DeriveArenaAddress(roundData);
                states = states.TransferAsset(context.Signer, arenaAdr, costCrystal);
            }

            // check medal
            if (roundData.ArenaType.Equals(ArenaType.Championship))
            {
                var medalCount = GetMedalTotalCount(row, avatarState);
                if (medalCount < roundData.RequiredMedalCount)
                {
                    throw new NotEnoughMedalException(
                        $"{nameof(JoinArena)} : have({medalCount}) < Required Medal Count({roundData.RequiredMedalCount}) ");
                }
            }

            // create ArenaScore
            var arenaScoreAdr = ArenaScore.DeriveAddress(avatarAddress, roundData.Id, roundData.Round);
            if (states.TryGetState(arenaScoreAdr, out List _))
            {
                throw new AlreadyEnteredException(
                    $"{nameof(ArenaScore)} : id({roundData.Id}) / round({roundData.Round})");
            }

            var arenaScore = new ArenaScore(avatarAddress, roundData.Id, roundData.Round);

            // create ArenaInformation
            var arenaInformationAdr = ArenaInformation.DeriveAddress(avatarAddress, roundData.Id, roundData.Round);
            if (states.TryGetState(arenaInformationAdr, out List _))
            {
                throw new AlreadyEnteredException(
                    $"{nameof(ArenaInformation)} : id({roundData.Id}) / round({roundData.Round})");
            }

            var arenaInformation = new ArenaInformation(avatarAddress, roundData.Id, roundData.Round);

            // update ArenaParticipants
            var arenaParticipantsAdr = ArenaParticipants.DeriveAddress(roundData.Id, roundData.Round);
            var arenaParticipants = GetArenaParticipants(states, arenaParticipantsAdr, roundData.Id, roundData.Round);
            arenaParticipants.Add(avatarAddress);

            // update ArenaAvatarState
            var arenaAvatarStateAdr = ArenaAvatarState.DeriveAddress(avatarAddress);
            var arenaAvatarState = GetArenaAvatarState(states, arenaAvatarStateAdr, avatarState);
            arenaAvatarState.UpdateCostumes(costumes);
            arenaAvatarState.UpdateEquipment(equipments);
            arenaAvatarState.UpdateLevel(avatarState.level);

            return states
                .SetState(arenaScoreAdr, arenaScore.Serialize())
                .SetState(arenaInformationAdr, arenaInformation.Serialize())
                .SetState(arenaParticipantsAdr, arenaParticipants.Serialize())
                .SetState(arenaAvatarStateAdr, arenaAvatarState.Serialize())
                .SetState(context.Signer, agentState.Serialize());
        }

        public static int GetMedalTotalCount(ArenaSheet.Row row, AvatarState avatarState)
        {
            var count = 0;
            foreach (var data in row.Round)
            {
                var itemId = GetMedalItemId(data.Id, data.Round);
                if (avatarState.inventory.TryGetItem(itemId, out var item))
                {
                    count += item.count;
                }
            }

            return count;
        }

        public static int GetMedalItemId(int id, int round)
        {
            return 700_000 + (id * 100) + round;
        }

        public static ArenaParticipants GetArenaParticipants(IAccountStateDelta states,
            Address arenaStateAddress, int id, int round)
        {
            return states.TryGetState(arenaStateAddress, out List list)
                ? new ArenaParticipants(list)
                : new ArenaParticipants(id, round);
        }

        public static ArenaAvatarState GetArenaAvatarState(IAccountStateDelta states,
            Address arenaAvatarStateAddress, AvatarState avatarState)
        {
            return states.TryGetState(arenaAvatarStateAddress, out List list)
                ? new ArenaAvatarState(list)
                : new ArenaAvatarState(avatarState);
        }
    }
}
