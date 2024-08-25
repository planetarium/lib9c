using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Exceptions.Arena;
using Nekoyume.Arena;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Rune;
using Serilog;

namespace Nekoyume.Action
{
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/2195
    /// </summary>
    [Serializable]
    [ActionType("join_arena4")]
    public class JoinArena : GameAction, IJoinArenaV1
    {
        public Address avatarAddress;
        public int championshipId;
        public int round;
        public List<Guid> costumes;
        public List<Guid> equipments;
        public List<RuneSlotInfo> runeInfos;

        Address IJoinArenaV1.AvatarAddress => avatarAddress;
        int IJoinArenaV1.ChampionshipId => championshipId;
        int IJoinArenaV1.Round => round;
        IEnumerable<Guid> IJoinArenaV1.Costumes => costumes;
        IEnumerable<Guid> IJoinArenaV1.Equipments => equipments;

        IEnumerable<IValue> IJoinArenaV1.RuneSlotInfos => runeInfos
            .Select(x => x.Serialize());

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
                ["runeInfos"] = runeInfos.OrderBy(x => x.SlotIndex).Select(x => x.Serialize()).Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            championshipId = plainValue["championshipId"].ToInteger();
            round = plainValue["round"].ToInteger();
            costumes = ((List)plainValue["costumes"]).Select(e => e.ToGuid()).ToList();
            equipments = ((List)plainValue["equipments"]).Select(e => e.ToGuid()).ToList();
            runeInfos = plainValue["runeInfos"].ToList(x => new RuneSlotInfo((List)x));
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}JoinArena exec started", addressesHex);

            var agentState = states.GetAgentState(context.Signer);
            if (agentState is null)
            {
                throw new FailedLoadStateException(
                    $"[{nameof(JoinArena)}] Aborted as the agent state of the signer was failed to load.");
            }

            if (!states.TryGetAvatarState(context.Signer, avatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException(
                    $"[{nameof(JoinArena)}] Aborted as the avatar state of the signer was failed to load.");
            }

            // check the avatar already joined the arena. 
            if (states.GetArenaParticipant(championshipId, round, avatarAddress) is not null)
            {
                throw new AlreadyJoinedArenaException(championshipId, round, avatarAddress);
            }

            var collectionExist =
                states.TryGetCollectionState(avatarAddress, out var collectionState) &&
                collectionState.Ids.Any();
            var sheetTypes = new List<Type>
            {
                typeof(ArenaSheet),
                typeof(CharacterSheet),
                typeof(CostumeStatSheet),
                typeof(EquipmentItemOptionSheet),
                typeof(EquipmentItemRecipeSheet),
                typeof(EquipmentItemSubRecipeSheetV2),
                typeof(ItemRequirementSheet),
                typeof(RuneLevelBonusSheet),
                typeof(RuneListSheet),
                typeof(RuneOptionSheet),
            };
            if (collectionExist)
            {
                sheetTypes.Add(typeof(CollectionSheet));
            }

            var sheets = states.GetSheets(sheetTypes: sheetTypes);
            var gameConfigState = states.GetGameConfigState();
            var (equipmentItems, costumeItems) = avatarState.ValidEquipmentAndCostumeV2(
                costumes,
                equipments,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                context.BlockIndex,
                addressesHex,
                gameConfigState);

            // update rune slot
            var runeSlotStateAddress = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Arena);
            var runeSlotState = states.TryGetLegacyState(runeSlotStateAddress, out List rawRuneSlotState)
                ? new RuneSlotState(rawRuneSlotState)
                : new RuneSlotState(BattleType.Arena);
            var runeListSheet = sheets.GetSheet<RuneListSheet>();
            runeSlotState.UpdateSlot(runeInfos, runeListSheet);
            states = states.SetLegacyState(runeSlotStateAddress, runeSlotState.Serialize());

            // update item slot
            var itemSlotStateAddress = ItemSlotState.DeriveAddress(avatarAddress, BattleType.Arena);
            var itemSlotState = states.TryGetLegacyState(itemSlotStateAddress, out List rawItemSlotState)
                ? new ItemSlotState(rawItemSlotState)
                : new ItemSlotState(BattleType.Arena);
            itemSlotState.UpdateEquipment(equipments);
            itemSlotState.UpdateCostumes(costumes);
            states = states.SetLegacyState(itemSlotStateAddress, itemSlotState.Serialize());

            var sheet = sheets.GetSheet<ArenaSheet>();
            if (!sheet.TryGetValue(championshipId, out var row))
            {
                throw new SheetRowNotFoundException(
                    nameof(ArenaSheet), $"championship Id : {championshipId}");
            }

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(JoinArena)}] ChampionshipId({row.ChampionshipId}) - round({round})");
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

                var arenaAddr = ArenaHelper.DeriveArenaAddress(roundData.ChampionshipId, roundData.Round);
                states = states.TransferAsset(context, context.Signer, arenaAddr, fee);
            }

            // check medal
            if (roundData.ArenaType.Equals(ArenaType.Championship))
            {
                var medalCount = ArenaHelper.GetMedalTotalCount(row, avatarState);
                if (medalCount < roundData.RequiredMedalCount)
                {
                    throw new NotEnoughMedalException(
                        $"[{nameof(JoinArena)}] Not enough medals to join the arena." +
                        $" Required: {roundData.RequiredMedalCount}, but has: {medalCount}");
                }
            }

            // create ArenaScore
            var arenaScoreAddr =
                ArenaScore.DeriveAddress(avatarAddress, roundData.ChampionshipId, roundData.Round);
            if (states.TryGetLegacyState(arenaScoreAddr, out List _))
            {
                throw new ArenaScoreAlreadyContainsException(
                    $"[{nameof(JoinArena)}] id({roundData.ChampionshipId}) / round({roundData.Round})");
            }

            var arenaScore = new ArenaScore(avatarAddress, roundData.ChampionshipId, roundData.Round);

            // create ArenaInformation
            var arenaInformationAddr =
                ArenaInformation.DeriveAddress(avatarAddress, roundData.ChampionshipId, roundData.Round);
            if (states.TryGetLegacyState(arenaInformationAddr, out List _))
            {
                throw new ArenaInformationAlreadyContainsException(
                    $"[{nameof(JoinArena)}] id({roundData.ChampionshipId}) / round({roundData.Round})");
            }

            var arenaInformation =
                new ArenaInformation(avatarAddress, roundData.ChampionshipId, roundData.Round);

            // update ArenaParticipants
            var arenaParticipantsAddr = ArenaParticipants.DeriveAddress(roundData.ChampionshipId, roundData.Round);
            var arenaParticipants =
                states.GetArenaParticipants(arenaParticipantsAddr, roundData.ChampionshipId, roundData.Round);
            arenaParticipants.Add(avatarAddress);

            // update ArenaAvatarState: It seems like a good idea to consolidate this into ItemSlotState.
            var arenaAvatarStateAddr = ArenaAvatarState.DeriveAddress(avatarAddress);
            var arenaAvatarState = states.GetArenaAvatarState(arenaAvatarStateAddr, avatarState);
            arenaAvatarState.UpdateCostumes(costumes);
            arenaAvatarState.UpdateEquipment(equipments);

            // start getting the total CP from here.
            var runeStates = states.GetRuneState(avatarAddress, out var migrateRequired);
            if (migrateRequired)
            {
                states = states.SetRuneState(avatarAddress, runeStates);
            }

            var equippedRune = new List<RuneState>();
            foreach (var runeInfo in runeSlotState.GetEquippedRuneSlotInfos())
            {
                if (runeStates.TryGetRuneState(runeInfo.RuneId, out var runeState))
                {
                    equippedRune.Add(runeState);
                }
            }

            var runeOptionSheet = sheets.GetSheet<RuneOptionSheet>();
            var runeOptions = new List<RuneOptionSheet.Row.RuneOptionInfo>();
            foreach (var runeState in equippedRune)
            {
                if (!runeOptionSheet.TryGetValue(runeState.RuneId, out var optionRow))
                {
                    throw new SheetRowNotFoundException("RuneOptionSheet", runeState.RuneId);
                }

                if (!optionRow.LevelOptionMap.TryGetValue(runeState.Level, out var option))
                {
                    throw new SheetRowNotFoundException("RuneOptionSheet", runeState.Level);
                }

                runeOptions.Add(option);
            }

            var characterSheet = sheets.GetSheet<CharacterSheet>();
            if (!characterSheet.TryGetValue(avatarState.characterId, out var characterRow))
            {
                throw new SheetRowNotFoundException("CharacterSheet", avatarState.characterId);
            }

            var costumeStatSheet = sheets.GetSheet<CostumeStatSheet>();
            var collectionModifiers = new List<StatModifier>();
            if (collectionExist)
            {
                var collectionSheet = sheets.GetSheet<CollectionSheet>();
                collectionModifiers = collectionState.GetModifiers(collectionSheet);
            }

            var runeLevelBonusSheet = sheets.GetSheet<RuneLevelBonusSheet>();
            var runeLevelBonus =
                RuneHelper.CalculateRuneLevelBonus(runeStates, runeListSheet, runeLevelBonusSheet);
            var cp = CPHelper.TotalCP(
                equipmentItems,
                costumeItems,
                runeOptions,
                avatarState.level,
                characterRow,
                costumeStatSheet,
                collectionModifiers,
                runeLevelBonus);

            // create ArenaParticipant: This is currently redundant, but we plan to replace all the ArenaScore and
            // ArenaInformation states and some of the ArenaAvatarState states in the future.
            var arenaParticipant = new ArenaParticipant(avatarAddress)
            {
                Name = avatarState.name,
                PortraitId = avatarState.GetPortraitId(),
                Level = avatarState.level,
                Cp = cp,
                Score = arenaScore.Score,
                Ticket = arenaInformation.Ticket,
                TicketResetCount = arenaInformation.TicketResetCount,
                PurchasedTicketCount = arenaInformation.PurchasedTicketCount,
                Win = arenaInformation.Win,
                Lose = arenaInformation.Lose,
                LastBattleBlockIndex = arenaAvatarState.LastBattleBlockIndex,
            };

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}JoinArena Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetLegacyState(arenaScoreAddr, arenaScore.Serialize())
                .SetLegacyState(arenaInformationAddr, arenaInformation.Serialize())
                .SetLegacyState(arenaParticipantsAddr, arenaParticipants.Serialize())
                .SetLegacyState(arenaAvatarStateAddr, arenaAvatarState.Serialize())
                .SetAgentState(context.Signer, agentState)
                .SetArenaParticipant(championshipId, round, avatarAddress, arenaParticipant);
        }
    }
}
