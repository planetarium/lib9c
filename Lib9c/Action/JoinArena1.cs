using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Nekoyume.Arena;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/1495
    /// </summary>
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V200020AccidentObsoleteIndex)]
    [ActionType("join_arena")]
    public class JoinArena1 : GameAction, IJoinArenaV1
    {
        public Address avatarAddress;
        public int championshipId;
        public int round;
        public List<Guid> costumes;
        public List<Guid> equipments;

        Address IJoinArenaV1.AvatarAddress => avatarAddress;
        int IJoinArenaV1.ChampionshipId => championshipId;
        int IJoinArenaV1.Round => round;
        IEnumerable<Guid> IJoinArenaV1.Costumes => costumes;
        IEnumerable<Guid> IJoinArenaV1.Equipments => equipments;

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

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;

            if (championshipId > 2)
            {
                throw new ActionObsoletedException();
            }

            CheckObsolete(ActionObsoleteConfig.V100340ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}JoinArena exec started", addressesHex);

            var agentState = states.GetAgentState(context.Signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException($"[{nameof(JoinArena1)}] Aborted as the agent state of the signer was failed to load.");
            }

            if (!states.TryGetAvatarState(context.Signer, avatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException($"[{nameof(JoinArena1)}] Aborted as the avatar state of the signer was failed to load.");
            }

            if (!avatarState.worldInformation.TryGetUnlockedWorldByStageClearedBlockIndex(
                    out var world))
            {
                throw new NotEnoughClearedStageLevelException(
                    $"{addressesHex}Aborted as NotEnoughClearedStageLevelException");
            }

            if (world.StageClearedId < GameConfig.RequireClearedStageLevel.ActionsInRankingBoard)
            {
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                    world.StageClearedId);
            }

            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(ItemRequirementSheet),
                    typeof(EquipmentItemRecipeSheet),
                    typeof(EquipmentItemSubRecipeSheetV2),
                    typeof(EquipmentItemOptionSheet),
                    typeof(ArenaSheet),
                });

            avatarState.ValidEquipmentAndCostume(costumes, equipments,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                context.BlockIndex, addressesHex);

            var sheet = sheets.GetSheet<ArenaSheet>();
            if (!sheet.TryGetValue(championshipId, out var row))
            {
                throw new SheetRowNotFoundException(
                    nameof(ArenaSheet), $"championship Id : {championshipId}");
            }

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(JoinArena1)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            // check fee

            var fee = ArenaHelper.GetEntranceFee(roundData, context.BlockIndex, avatarState.level);
            if (fee > 0 * CrystalCalculator.CRYSTAL)
            {
                var crystalBalance = states.GetBalance(context.Signer, CrystalCalculator.CRYSTAL);
                if (fee > crystalBalance)
                {
                    throw new NotEnoughFungibleAssetValueException(
                        $"required {fee}, but balance is {crystalBalance}");
                }

var feeAddress = states.GetFeeAddress(context.BlockIndex);

                states = states.TransferAsset(context, context.Signer, feeAddress, fee);
            }

            // check medal
            if (roundData.ArenaType.Equals(ArenaType.Championship))
            {
                var medalCount = ArenaHelper.GetMedalTotalCount(row, avatarState);
                if (medalCount < roundData.RequiredMedalCount)
                {
                    throw new NotEnoughMedalException(
                        $"[{nameof(JoinArena1)}] have({medalCount}) < Required Medal Count({roundData.RequiredMedalCount}) ");
                }
            }

            // create ArenaScore
            var arenaScoreAdr =
                ArenaScore.DeriveAddress(avatarAddress, roundData.ChampionshipId, roundData.Round);
            if (states.TryGetLegacyState(arenaScoreAdr, out List _))
            {
                throw new ArenaScoreAlreadyContainsException(
                    $"[{nameof(JoinArena1)}] id({roundData.ChampionshipId}) / round({roundData.Round})");
            }

            var arenaScore = new ArenaScore(avatarAddress, roundData.ChampionshipId, roundData.Round);

            // create ArenaInformation
            var arenaInformationAdr =
                ArenaInformation.DeriveAddress(avatarAddress, roundData.ChampionshipId, roundData.Round);
            if (states.TryGetLegacyState(arenaInformationAdr, out List _))
            {
                throw new ArenaInformationAlreadyContainsException(
                    $"[{nameof(JoinArena1)}] id({roundData.ChampionshipId}) / round({roundData.Round})");
            }

            var arenaInformation =
                new ArenaInformation(avatarAddress, roundData.ChampionshipId, roundData.Round);

            // update ArenaParticipants
            var arenaParticipantsAdr = ArenaParticipants.DeriveAddress(roundData.ChampionshipId, roundData.Round);
            var arenaParticipants = states.GetArenaParticipants(arenaParticipantsAdr, roundData.ChampionshipId, roundData.Round);
            arenaParticipants.Add(avatarAddress);

            // update ArenaAvatarState
            var arenaAvatarStateAdr = ArenaAvatarState.DeriveAddress(avatarAddress);
            var arenaAvatarState = states.GetArenaAvatarState(arenaAvatarStateAdr, avatarState);
            arenaAvatarState.UpdateCostumes(costumes);
            arenaAvatarState.UpdateEquipment(equipments);

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}JoinArena Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetLegacyState(arenaScoreAdr, arenaScore.Serialize())
                .SetLegacyState(arenaInformationAdr, arenaInformation.Serialize())
                .SetLegacyState(arenaParticipantsAdr, arenaParticipants.Serialize())
                .SetLegacyState(arenaAvatarStateAdr, arenaAvatarState.Serialize())
                .SetAgentState(context.Signer, agentState);
        }
    }
}
