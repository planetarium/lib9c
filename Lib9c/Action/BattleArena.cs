using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Arena;
using Nekoyume.Battle;
using Nekoyume.Exceptions;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.Arena;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Rune;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Introduce at https://github.com/planetarium/lib9c/pull/2229
    /// Changed at https://github.com/planetarium/lib9c/pull/2242
    /// </summary>
    [Serializable]
    [ActionType("battle_arena15")]
    public class BattleArena : GameAction, IBattleArenaV1
    {
        public const string PurchasedCountKey = "purchased_count_during_interval";
        public const int HpIncreasingModifier = 5;
        public Address myAvatarAddress;
        public Address enemyAvatarAddress;
        public int championshipId;
        public int round;
        public int ticket;

        public List<Guid> costumes;
        public List<Guid> equipments;
        public List<RuneSlotInfo> runeInfos;

        Address IBattleArenaV1.MyAvatarAddress => myAvatarAddress;

        Address IBattleArenaV1.EnemyAvatarAddress => enemyAvatarAddress;

        int IBattleArenaV1.ChampionshipId => championshipId;

        int IBattleArenaV1.Round => round;

        int IBattleArenaV1.Ticket => ticket;

        IEnumerable<Guid> IBattleArenaV1.Costumes => costumes;

        IEnumerable<Guid> IBattleArenaV1.Equipments => equipments;

        IEnumerable<IValue> IBattleArenaV1.RuneSlotInfos => runeInfos
            .Select(x => x.Serialize());

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>()
            {
                [MyAvatarAddressKey] = myAvatarAddress.Serialize(),
                [EnemyAvatarAddressKey] = enemyAvatarAddress.Serialize(),
                [ChampionshipIdKey] = championshipId.Serialize(),
                [RoundKey] = round.Serialize(),
                [TicketKey] = ticket.Serialize(),
                [CostumesKey] = new List(costumes
                    .OrderBy(element => element).Select(e => e.Serialize())),
                [EquipmentsKey] = new List(equipments
                    .OrderBy(element => element).Select(e => e.Serialize())),
                [RuneInfos] = runeInfos.OrderBy(x => x.SlotIndex).Select(x=> x.Serialize()).Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            myAvatarAddress = plainValue[MyAvatarAddressKey].ToAddress();
            enemyAvatarAddress = plainValue[EnemyAvatarAddressKey].ToAddress();
            championshipId = plainValue[ChampionshipIdKey].ToInteger();
            round = plainValue[RoundKey].ToInteger();
            ticket = plainValue[TicketKey].ToInteger();
            costumes = ((List) plainValue[CostumesKey]).Select(e => e.ToGuid()).ToList();
            equipments = ((List) plainValue[EquipmentsKey]).Select(e => e.ToGuid()).ToList();
            runeInfos = plainValue[RuneInfos].ToList(x => new RuneSlotInfo((List) x));
            ValidateTicket();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            ValidateTicket();
            var states = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(
                context,
                myAvatarAddress,
                enemyAvatarAddress);

            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}BattleArena exec started", addressesHex);
            if (myAvatarAddress.Equals(enemyAvatarAddress))
            {
                throw new InvalidAddressException(
                    $"{addressesHex}Aborted as the signer tried to battle for themselves.");
            }

            if (!states.TryGetAvatarState(
                    context.Signer,
                    myAvatarAddress,
                    out var myAvatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var collectionStates =
                states.GetCollectionStates(new[]{ myAvatarAddress, enemyAvatarAddress });
            var collectionExist = collectionStates.Count > 0;
            var sheetTypes = new List<Type>
            {
                typeof(ArenaSheet),
                typeof(ItemRequirementSheet),
                typeof(EquipmentItemRecipeSheet),
                typeof(EquipmentItemSubRecipeSheetV2),
                typeof(EquipmentItemOptionSheet),
                typeof(MaterialItemSheet),
                typeof(RuneListSheet),
                typeof(RuneLevelBonusSheet),
                typeof(BuffLimitSheet),
                typeof(BuffLinkSheet),
            };
            if (collectionExist)
            {
                sheetTypes.Add(typeof(CollectionSheet));
            }
            var sheets = states.GetSheets(
                containArenaSimulatorSheets: true,
                sheetTypes: sheetTypes);

            var gameConfigState = states.GetGameConfigState();
            var (equipmentItems, costumeItems) = myAvatarState.ValidEquipmentAndCostumeV2(
                costumes,
                equipments,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                context.BlockIndex,
                addressesHex,
                gameConfigState);

            // update my rune slot
            var myRuneSlotStateAddress = RuneSlotState.DeriveAddress(myAvatarAddress, BattleType.Arena);
            var myRuneSlotState = states.TryGetLegacyState(myRuneSlotStateAddress, out List rawRuneSlotState)
                ? new RuneSlotState(rawRuneSlotState)
                : new RuneSlotState(BattleType.Arena);
            var runeListSheet = sheets.GetSheet<RuneListSheet>();
            myRuneSlotState.UpdateSlot(runeInfos, runeListSheet);
            states = states.SetLegacyState(myRuneSlotStateAddress, myRuneSlotState.Serialize());

            // update my item slot
            var myItemSlotStateAddress = ItemSlotState.DeriveAddress(myAvatarAddress, BattleType.Arena);
            var myItemSlotState = states.TryGetLegacyState(myItemSlotStateAddress, out List rawItemSlotState)
                ? new ItemSlotState(rawItemSlotState)
                : new ItemSlotState(BattleType.Arena);
            myItemSlotState.UpdateEquipment(equipments);
            myItemSlotState.UpdateCostumes(costumes);
            states = states.SetLegacyState(myItemSlotStateAddress, myItemSlotState.Serialize());

            // check championship id and round in ArenaSheet.
            var arenaSheet = sheets.GetSheet<ArenaSheet>();
            if (!arenaSheet.TryGetValue(championshipId, out var arenaRow))
            {
                throw new SheetRowNotFoundException(nameof(ArenaSheet),
                    $"championship Id : {championshipId}");
            }

            if (!arenaRow.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({arenaRow.ChampionshipId}) - " +
                    $"round({round})");
            }

            if (!roundData.IsTheRoundOpened(context.BlockIndex))
            {
                throw new ThisArenaIsClosedException(
                    $"{nameof(BattleArena)} : block index({context.BlockIndex}) - " +
                    $"championshipId({roundData.ChampionshipId}) - round({roundData.Round})");
            }

            // check both myAvatarAddress and enemyAvatarAddress are joined in the current arena round.
            var arenaParticipantsAddr = ArenaParticipants.DeriveAddress(roundData.ChampionshipId, roundData.Round);
            if (!states.TryGetArenaParticipants(arenaParticipantsAddr, out var arenaParticipants))
            {
                throw new ArenaParticipantsNotFoundException(
                    $"[{nameof(BattleArena)}] ChampionshipId({roundData.ChampionshipId}) - " +
                    $"round({roundData.Round})");
            }

            if (!arenaParticipants.AvatarAddresses.Contains(myAvatarAddress))
            {
                throw new AddressNotFoundInArenaParticipantsException(
                    $"[{nameof(BattleArena)}] my avatar address : {myAvatarAddress}");
            }

            if (!arenaParticipants.AvatarAddresses.Contains(enemyAvatarAddress))
            {
                throw new AddressNotFoundInArenaParticipantsException(
                    $"[{nameof(BattleArena)}] enemy avatar address : {enemyAvatarAddress}");
            }

            // check last battle block index of my arena avatar state to prevent frequent battles.
            var myArenaAvatarStateAddr = ArenaAvatarState.DeriveAddress(myAvatarAddress);
            if (!states.TryGetArenaAvatarState(myArenaAvatarStateAddr, out var myArenaAvatarState))
            {
                throw new ArenaAvatarStateNotFoundException(
                    $"[{nameof(BattleArena)}] my avatar address : {myAvatarAddress}");
            }

            var battleArenaInterval = roundData.ArenaType == ArenaType.OffSeason
                ? 1
                : gameConfigState.BattleArenaInterval;
            if (context.BlockIndex - myArenaAvatarState.LastBattleBlockIndex < battleArenaInterval)
            {
                throw new CoolDownBlockException(
                    $"[{nameof(BattleArena)}] LastBattleBlockIndex : " +
                    $"{myArenaAvatarState.LastBattleBlockIndex} " +
                    $"CurrentBlockIndex : {context.BlockIndex}");
            }

            // check my arena score and enemy arena score are within an acceptable range to proceed with the battle.
            var myArenaScoreAddr = ArenaScore.DeriveAddress(
                myAvatarAddress,
                roundData.ChampionshipId,
                roundData.Round);
            if (!states.TryGetArenaScore(myArenaScoreAddr, out var myArenaScore))
            {
                throw new ArenaScoreNotFoundException(
                    $"[{nameof(BattleArena)}] my avatar address : {myAvatarAddress}" +
                    $" - ChampionshipId({roundData.ChampionshipId}) - round({roundData.Round})");
            }

            var enemyArenaScoreAddr = ArenaScore.DeriveAddress(
                enemyAvatarAddress,
                roundData.ChampionshipId,
                roundData.Round);
            if (!states.TryGetArenaScore(enemyArenaScoreAddr, out var enemyArenaScore))
            {
                throw new ArenaScoreNotFoundException(
                    $"[{nameof(BattleArena)}] enemy avatar address : {enemyAvatarAddress}" +
                    $" - ChampionshipId({roundData.ChampionshipId}) - round({roundData.Round})");
            }

            if (!ArenaHelper.ValidateScoreDifference(
                    ArenaHelper.ScoreLimits,
                    roundData.ArenaType,
                    myArenaScore.Score,
                    enemyArenaScore.Score))
            {
                var scoreDiff = enemyArenaScore.Score - myArenaScore.Score;
                throw new ValidateScoreDifferenceException(
                    $"[{nameof(BattleArena)}] Arena Type({roundData.ArenaType}) : " +
                    $"enemyScore({enemyArenaScore.Score}) - myScore({myArenaScore.Score}) = " +
                    $"diff({scoreDiff})");
            }

            var myArenaInformationAddr = ArenaInformation.DeriveAddress(
                myAvatarAddress,
                roundData.ChampionshipId,
                roundData.Round);
            if (!states.TryGetArenaInformation(myArenaInformationAddr, out var myArenaInformation))
            {
                throw new ArenaInformationNotFoundException(
                    $"[{nameof(BattleArena)}] my avatar address : {myAvatarAddress}" +
                    $" - ChampionshipId({roundData.ChampionshipId}) - round({roundData.Round})");
            }

            var purchasedCountAddr = myArenaInformation.Address.Derive(PurchasedCountKey);
            if (!states.TryGetLegacyState(purchasedCountAddr, out Integer purchasedCountDuringInterval))
            {
                purchasedCountDuringInterval = 0;
            }

            var dailyArenaInterval = gameConfigState.DailyArenaInterval;
            var currentTicketResetCount = ArenaHelper.GetCurrentTicketResetCount(
                context.BlockIndex,
                roundData.StartBlockIndex,
                dailyArenaInterval);
            if (myArenaInformation.TicketResetCount < currentTicketResetCount)
            {
                myArenaInformation.ResetTicket(currentTicketResetCount);
                purchasedCountDuringInterval = 0;
                states = states.SetLegacyState(purchasedCountAddr, purchasedCountDuringInterval);
            }

            if (roundData.ArenaType != ArenaType.OffSeason && ticket > 1)
            {
                throw new ExceedPlayCountException(
                    $"[{nameof(BattleArena)}] ticket : {ticket} / arenaType : {roundData.ArenaType}");
            }

            if (myArenaInformation.Ticket > 0)
            {
                myArenaInformation.UseTicket(ticket);
            }
            else if (ticket > 1)
            {
                throw new TicketPurchaseLimitExceedException(
                    $"[{nameof(ArenaInformation)}] tickets to buy : {ticket}");
            }
            else
            {
                var arenaAddr =
                    ArenaHelper.DeriveArenaAddress(roundData.ChampionshipId, roundData.Round);
                var goldCurrency = states.GetGoldCurrency();
                var ticketBalance =
                    ArenaHelper.GetTicketPrice(roundData, myArenaInformation, goldCurrency);
                myArenaInformation.BuyTicket(roundData.MaxPurchaseCount);
                if (purchasedCountDuringInterval >= roundData.MaxPurchaseCountWithInterval)
                {
                    throw new ExceedTicketPurchaseLimitDuringIntervalException(
                        $"[{nameof(ArenaInformation)}] PurchasedTicketCount({purchasedCountDuringInterval}) >= MAX({{max}})");
                }

                purchasedCountDuringInterval++;
                states = states
                    .TransferAsset(context, context.Signer, arenaAddr, ticketBalance)
                    .SetLegacyState(purchasedCountAddr, purchasedCountDuringInterval);
            }

            // update my arena avatar state
            myArenaAvatarState.UpdateEquipment(equipments);
            myArenaAvatarState.UpdateCostumes(costumes);
            myArenaAvatarState.LastBattleBlockIndex = context.BlockIndex;
            var myRuneStates = states.GetRuneState(myAvatarAddress, out var migrateRequired);
            if (migrateRequired)
            {
                states = states.SetRuneState(myAvatarAddress, myRuneStates);
            }

            // get enemy equipped items
            var enemyItemSlotStateAddress = ItemSlotState.DeriveAddress(enemyAvatarAddress, BattleType.Arena);
            var enemyItemSlotState = states.TryGetLegacyState(enemyItemSlotStateAddress, out List rawEnemyItemSlotState)
                ? new ItemSlotState(rawEnemyItemSlotState)
                : new ItemSlotState(BattleType.Arena);
            var enemyRuneSlotStateAddress = RuneSlotState.DeriveAddress(enemyAvatarAddress, BattleType.Arena);
            var enemyRuneSlotState = states.TryGetLegacyState(enemyRuneSlotStateAddress, out List enemyRawRuneSlotState)
                ? new RuneSlotState(enemyRawRuneSlotState)
                : new RuneSlotState(BattleType.Arena);
            var enemyRuneStates = states.GetRuneState(enemyAvatarAddress, out _);

            // simulate
            var myArenaPlayerDigest = new ArenaPlayerDigest(
                myAvatarState,
                equipments,
                costumes,
                myRuneStates,
                myRuneSlotState);
            var enemyAvatarState = states.GetEnemyAvatarState(enemyAvatarAddress);
            var enemyArenaPlayerDigest = new ArenaPlayerDigest(
                enemyAvatarState,
                enemyItemSlotState.Equipments,
                enemyItemSlotState.Costumes,
                enemyRuneStates,
                enemyRuneSlotState);
            var previousMyScore = myArenaScore.Score;
            var arenaSheets = sheets.GetArenaSimulatorSheets();
            var buffLimitSheet = sheets.GetSheet<BuffLimitSheet>();
            var winCount = 0;
            var defeatCount = 0;
            var rewards = new List<ItemBase>();
            var random = context.GetRandom();
            var collectionModifiers = new Dictionary<Address, List<StatModifier>>
            {
                [myAvatarAddress] = new(),
                [enemyAvatarAddress] = new(),
            };
            if (collectionExist)
            {
                var collectionSheet = sheets.GetSheet<CollectionSheet>();
#pragma warning disable LAA1002
                foreach (var (address, state) in collectionStates)
#pragma warning restore LAA1002
                {
                    collectionModifiers[address] = state.GetModifiers(collectionSheet);
                }
            }

            var buffLinkSheet = sheets.GetSheet<BuffLinkSheet>();
            for (var i = 0; i < ticket; i++)
            {
                var simulator = new ArenaSimulator(random, HpIncreasingModifier,
                    gameConfigState.ShatterStrikeMaxDamage);
                var log = simulator.Simulate(
                    myArenaPlayerDigest,
                    enemyArenaPlayerDigest,
                    arenaSheets,
                    collectionModifiers[myAvatarAddress],
                    collectionModifiers[enemyAvatarAddress],
                    buffLimitSheet,
                    buffLinkSheet,
                    true);
                if (log.Result.Equals(ArenaLog.ArenaResult.Win))
                {
                    winCount++;
                }
                else
                {
                    defeatCount++;
                }

                var reward = RewardSelector.Select(
                    random,
                    sheets.GetSheet<WeeklyArenaRewardSheet>(),
                    sheets.GetSheet<MaterialItemSheet>(),
                    myArenaPlayerDigest.Level,
                    maxCount: ArenaHelper.GetRewardCount(previousMyScore));
                rewards.AddRange(reward);
            }

            // add rewards
            foreach (var itemBase in rewards.OrderBy(x => x.Id))
            {
                myAvatarState.inventory.AddItem(itemBase);
            }

            // add medals
            if (roundData.ArenaType != ArenaType.OffSeason && winCount > 0)
            {
                if (roundData.MedalId == 0)
                {
                    throw new MedalIdNotFoundException($"{addressesHex}{roundData.ChampionshipId}-{roundData.Round}.MedalId is zero. Need to set MedalId column at ArenaSheet.");
                }

                var materialSheet = sheets.GetSheet<MaterialItemSheet>();
                var medal = ItemFactory.CreateMaterial(materialSheet, roundData.MedalId);

                myAvatarState.inventory.AddItem(medal, count: winCount);
            }

            // update scores and record
            var (myWinScore, myDefeatScore, enemyDefeatScore) =
                ArenaHelper.GetScores(previousMyScore, enemyArenaScore.Score);
            var myScore = (myWinScore * winCount) + (myDefeatScore * defeatCount);
            myArenaScore.AddScore(myScore);
            enemyArenaScore.AddScore(enemyDefeatScore * winCount);
            myArenaInformation.UpdateRecord(winCount, defeatCount);

            // start getting the total my CP from here.
            var runeOptionSheet = sheets.GetSheet<RuneOptionSheet>();
            var myRuneOptions = new List<RuneOptionSheet.Row.RuneOptionInfo>();
            foreach (var runeInfo in myRuneSlotState.GetEquippedRuneSlotInfos())
            {
                if (!myRuneStates.TryGetRuneState(runeInfo.RuneId, out var runeState))
                {
                    continue;
                }

                if (!runeOptionSheet.TryGetValue(runeState.RuneId, out var optionRow))
                {
                    throw new SheetRowNotFoundException("RuneOptionSheet", runeState.RuneId);
                }

                if (!optionRow.LevelOptionMap.TryGetValue(runeState.Level, out var option))
                {
                    throw new SheetRowNotFoundException("RuneOptionSheet", runeState.Level);
                }

                myRuneOptions.Add(option);
            }

            var characterSheet = sheets.GetSheet<CharacterSheet>();
            if (!characterSheet.TryGetValue(myAvatarState.characterId, out var myCharacterRow))
            {
                throw new SheetRowNotFoundException("CharacterSheet", myAvatarState.characterId);
            }

            var costumeStatSheet = sheets.GetSheet<CostumeStatSheet>();
            var runeLevelBonusSheet = sheets.GetSheet<RuneLevelBonusSheet>();
            var myRuneLevelBonus = RuneHelper.CalculateRuneLevelBonus(myRuneStates, runeListSheet, runeLevelBonusSheet);
            var myCp = CPHelper.TotalCP(
                equipmentItems,
                costumeItems,
                myRuneOptions,
                myAvatarState.level,
                myCharacterRow,
                costumeStatSheet,
                collectionModifiers[myAvatarAddress],
                myRuneLevelBonus);

            // update myArenaParticipant: This is currently redundant, but we plan to replace all the ArenaScore and
            // ArenaInformation states and some of the ArenaAvatarState states in the future.
            // The reason we are creating a new ArenaParticipant instead of getting it and updating its state is to
            // save resources on getting it since we already have most of the values.
            var myArenaParticipant = new ArenaParticipant(myAvatarAddress)
            {
                Name = myAvatarState.name,
                PortraitId = myAvatarState.GetPortraitId(),
                Level = myAvatarState.level,
                Cp = myCp,
                Score = myArenaScore.Score,
                Ticket = myArenaInformation.Ticket,
                TicketResetCount = myArenaInformation.TicketResetCount,
                PurchasedTicketCount = myArenaInformation.PurchasedTicketCount,
                Win = myArenaInformation.Win,
                Lose = myArenaInformation.Lose,
                LastBattleBlockIndex = myArenaAvatarState.LastBattleBlockIndex,
            };
            states = states.SetArenaParticipant(championshipId, round, myAvatarAddress, myArenaParticipant);

            // update enemyArenaParticipantState.Score
            var enemyArenaParticipant = states.GetArenaParticipant(championshipId, round, enemyAvatarAddress);
            if (enemyArenaParticipant is not null)
            {
                enemyArenaParticipant.Score = enemyArenaScore.Score;
                states = states.SetArenaParticipant(championshipId, round, enemyAvatarAddress, enemyArenaParticipant);
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}BattleArena Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetLegacyState(myArenaAvatarStateAddr, myArenaAvatarState.Serialize())
                .SetLegacyState(myArenaScoreAddr, myArenaScore.Serialize())
                .SetLegacyState(enemyArenaScoreAddr, enemyArenaScore.Serialize())
                .SetLegacyState(myArenaInformationAddr, myArenaInformation.Serialize())
                .SetAvatarState(myAvatarAddress, myAvatarState);
        }

        private void ValidateTicket()
        {
            if (ticket <= 0)
            {
                throw new ArgumentException("ticket must be greater than 0");
            }
        }
    }
}
