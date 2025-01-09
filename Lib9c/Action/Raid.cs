using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Nekoyume.Arena;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
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
    /// Hard forked at https://github.com/planetarium/lib9c/pull/2195
    /// </summary>
    [Serializable]
    [ActionType("raid7")]
    public class Raid : GameAction, IRaidV2
    {
        public Address AvatarAddress;
        public List<Guid> EquipmentIds;
        public List<Guid> CostumeIds;
        public List<Guid> FoodIds;
        public List<RuneSlotInfo> RuneInfos;
        public bool PayNcg;

        Address IRaidV2.AvatarAddress => AvatarAddress;
        IEnumerable<Guid> IRaidV2.EquipmentIds => EquipmentIds;
        IEnumerable<Guid> IRaidV2.CostumeIds => CostumeIds;
        IEnumerable<Guid> IRaidV2.FoodIds => FoodIds;
        IEnumerable<IValue> IRaidV2.RuneSlotInfos => RuneInfos.Select(x => x.Serialize());
        bool IRaidV2.PayNcg => PayNcg;

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            IWorld states = context.PreviousState;
            var addressHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressHex}Raid exec started", addressHex);
            if (!states.TryGetAvatarState(
                context.Signer,
                AvatarAddress,
                out AvatarState avatarState))
            {
                throw new FailedLoadStateException(
                    $"Aborted as the avatar state of the signer was failed to load.");
            }

            var collectionExist = states.TryGetCollectionState(AvatarAddress, out var collectionState) && collectionState.Ids.Any();
            var sheetTypes = new List<Type>
            {
                typeof(MaterialItemSheet),
                typeof(SkillSheet),
                typeof(SkillBuffSheet),
                typeof(StatBuffSheet),
                typeof(CharacterLevelSheet),
                typeof(EquipmentItemSetEffectSheet),
                typeof(ItemRequirementSheet),
                typeof(EquipmentItemRecipeSheet),
                typeof(EquipmentItemSubRecipeSheetV2),
                typeof(EquipmentItemOptionSheet),
                typeof(WorldBossCharacterSheet),
                typeof(WorldBossListSheet),
                typeof(WorldBossGlobalHpSheet),
                typeof(WorldBossActionPatternSheet),
                typeof(CharacterSheet),
                typeof(CostumeStatSheet),
                typeof(RuneWeightSheet),
                typeof(WorldBossKillRewardSheet),
                typeof(RuneSheet),
                typeof(RuneListSheet),
                typeof(RuneLevelBonusSheet),
                typeof(BuffLimitSheet),
                typeof(BuffLinkSheet),
                typeof(MaterialItemSheet),
            };
            if (collectionExist)
            {
                sheetTypes.Add(typeof(CollectionSheet));
            }

            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(
                containRaidSimulatorSheets: true,
                sheetTypes: sheetTypes);
            var worldBossListSheet = sheets.GetSheet<WorldBossListSheet>();
            var row = worldBossListSheet.FindRowByBlockIndex(context.BlockIndex);
            int raidId = row.Id;
            Address worldBossAddress = Addresses.GetWorldBossAddress(raidId);
            Address raiderAddress = Addresses.GetRaiderAddress(AvatarAddress, raidId);
            // Check challenge count.
            RaiderState raiderState;
            if (states.TryGetLegacyState(raiderAddress, out List rawState))
            {
                raiderState = new RaiderState(rawState);
            }
            else
            {
                raiderState = new RaiderState();
                if (row.EntranceFee > 0)
                {
                    FungibleAssetValue crystalCost = CrystalCalculator.CalculateEntranceFee(avatarState.level, row.EntranceFee);
                    states = states.TransferAsset(context, context.Signer, worldBossAddress, crystalCost);
                }
                Address raiderListAddress = Addresses.GetRaiderListAddress(raidId);
                List<Address> raiderList =
                    states.TryGetLegacyState(raiderListAddress, out List rawRaiderList)
                        ? rawRaiderList.ToList(StateExtensions.ToAddress)
                        : new List<Address>();
                raiderList.Add(raiderAddress);
                states = states.SetLegacyState(raiderListAddress,
                    new List(raiderList.Select(a => a.Serialize())));
            }

            var gameConfigState = states.GetGameConfigState();
            if (context.BlockIndex - raiderState.UpdatedBlockIndex < gameConfigState.WorldBossRequiredInterval)
            {
                throw new RequiredBlockIntervalException($"wait for interval. {context.BlockIndex - raiderState.UpdatedBlockIndex}");
            }

            if (WorldBossHelper.CanRefillTicket(context.BlockIndex, raiderState.RefillBlockIndex,
                    row.StartedBlockIndex, gameConfigState.DailyWorldBossInterval))
            {
                raiderState.RemainChallengeCount = WorldBossHelper.MaxChallengeCount;
                raiderState.RefillBlockIndex = context.BlockIndex;
            }

            if (raiderState.RemainChallengeCount < 1)
            {
                if (PayNcg)
                {
                    if (raiderState.PurchaseCount >= row.MaxPurchaseCount)
                    {
                        throw new ExceedTicketPurchaseLimitException("");
                    }
                    var goldCurrency = states.GetGoldCurrency();

                    var feeAddress = states.GetFeeAddress(context.BlockIndex);

                    states = states.TransferAsset(context, context.Signer, feeAddress,
                        WorldBossHelper.CalculateTicketPrice(row, raiderState, goldCurrency));
                    raiderState.PurchaseCount++;
                }
                else
                {
                    throw new ExceedPlayCountException("");
                }
            }

            // Validate equipment, costume.
            var equipmentList = avatarState.ValidateEquipmentsV3(
                EquipmentIds, context.BlockIndex, gameConfigState);
            var foodIds = avatarState.ValidateConsumableV2(
                FoodIds, context.BlockIndex, gameConfigState);
            var costumeList = avatarState.ValidateCostumeV2(CostumeIds, gameConfigState);

            // Update rune slot
            var runeSlotStateAddress = RuneSlotState.DeriveAddress(AvatarAddress, BattleType.Raid);
            var runeSlotState = states.TryGetLegacyState(runeSlotStateAddress, out List rawRuneSlotState)
                ? new RuneSlotState(rawRuneSlotState)
                : new RuneSlotState(BattleType.Raid);
            var runeListSheet = sheets.GetSheet<RuneListSheet>();
            runeSlotState.UpdateSlot(RuneInfos, runeListSheet);
            states = states.SetLegacyState(runeSlotStateAddress, runeSlotState.Serialize());

            // Update item slot
            var itemSlotStateAddress = ItemSlotState.DeriveAddress(AvatarAddress, BattleType.Raid);
            var itemSlotState = states.TryGetLegacyState(itemSlotStateAddress, out List rawItemSlotState)
                ? new ItemSlotState(rawItemSlotState)
                : new ItemSlotState(BattleType.Raid);
            itemSlotState.UpdateEquipment(EquipmentIds);
            itemSlotState.UpdateCostumes(CostumeIds);
            states = states.SetLegacyState(itemSlotStateAddress, itemSlotState.Serialize());

            long previousHighScore = raiderState.HighScore;
            WorldBossState bossState;
            WorldBossGlobalHpSheet hpSheet = sheets.GetSheet<WorldBossGlobalHpSheet>();
            if (states.TryGetLegacyState(worldBossAddress, out List rawBossState))
            {
                bossState = new WorldBossState(rawBossState);
            }
            else
            {
                bossState = new WorldBossState(row, hpSheet[1]);
            }

            var addressesHex = $"[{context.Signer.ToHex()}, {AvatarAddress.ToHex()}]";
            var items = EquipmentIds.Concat(CostumeIds);
            avatarState.EquipItems(items);
            avatarState.ValidateItemRequirement(
                costumeList.Select(e => e.Id).Concat(foodIds).ToList(),
                equipmentList,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                addressesHex);

            var raidSimulatorSheets = sheets.GetRaidSimulatorSheets();
            var runeStates = states.GetRuneState(AvatarAddress, out var migrateRequired);
            if (migrateRequired)
            {
                states = states.SetRuneState(AvatarAddress, runeStates);
            }

            // just validate
            foreach (var runeSlotInfo in RuneInfos)
            {
                runeStates.GetRuneState(runeSlotInfo.RuneId);
            }

            var collectionModifiers = new List<StatModifier>();
            if (collectionExist)
            {
                var collectionSheet = sheets.GetSheet<CollectionSheet>();
                collectionModifiers = collectionState.GetModifiers(collectionSheet);
            }
            // Simulate.
            var random = context.GetRandom();
            var simulator = new RaidSimulator(
                row.BossId,
                random,
                avatarState,
                FoodIds,
                runeStates,
                runeSlotState,
                raidSimulatorSheets,
                sheets.GetSheet<CostumeStatSheet>(),
                collectionModifiers,
                sheets.GetSheet<BuffLimitSheet>(),
                sheets.GetSheet<BuffLinkSheet>(),
                shatterStrikeMaxDamage: gameConfigState.ShatterStrikeMaxDamage
            );
            simulator.Simulate();
            avatarState.inventory = simulator.Player.Inventory;

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
            var runeLevelBonus = RuneHelper.CalculateRuneLevelBonus(
                runeStates, sheets.GetSheet<RuneListSheet>(), sheets.GetSheet<RuneLevelBonusSheet>()
            );
            var cp = CPHelper.TotalCP(
                equipmentList, costumeList,
                runeOptions, avatarState.level,
                characterRow, costumeStatSheet, collectionModifiers,
                runeLevelBonus
                );
            long score = simulator.DamageDealt;
            raiderState.Update(avatarState, cp, score, PayNcg, context.BlockIndex);

            // Reward.
            bossState.CurrentHp -= score;
            if (bossState.CurrentHp <= 0)
            {
                if (bossState.Level < hpSheet.OrderedList.Last().Level)
                {
                    bossState.Level++;
                }
                bossState.CurrentHp = hpSheet[bossState.Level].Hp;
            }

            // battle reward
            foreach (var battleReward in simulator.AssetReward)
            {
                if (battleReward.Currency.Equals(CrystalCalculator.CRYSTAL))
                {
                    states = states.MintAsset(context, context.Signer, battleReward);
                }
                else
                {
                    states = states.MintAsset(context, AvatarAddress, battleReward);
                }
            }

            foreach (var battleReward in simulator.Reward)
            {
                avatarState.inventory.AddItem(battleReward);
            }

            if (raiderState.LatestBossLevel < bossState.Level)
            {
                // kill reward
                var worldBossKillRewardRecordAddress = Addresses.GetWorldBossKillRewardRecordAddress(AvatarAddress, raidId);
                WorldBossKillRewardRecord rewardRecord;
                if (states.TryGetLegacyState(worldBossKillRewardRecordAddress, out List rawList))
                {
                    var bossRow = raidSimulatorSheets.WorldBossCharacterSheet[row.BossId];
                    rewardRecord = new WorldBossKillRewardRecord(rawList);
                    // calculate with previous high score.
                    int rank = WorldBossHelper.CalculateRank(bossRow, previousHighScore);
                    states = states.SetWorldBossKillReward(
                        context,
                        worldBossKillRewardRecordAddress,
                        rewardRecord,
                        rank,
                        bossState,
                        sheets.GetSheet<RuneWeightSheet>(),
                        sheets.GetSheet<WorldBossKillRewardSheet>(),
                        sheets.GetSheet<RuneSheet>(),
                        sheets.GetSheet<MaterialItemSheet>(),
                        random,
                        avatarState.inventory,
                        AvatarAddress,
                        context.Signer
                    );
                }
                else
                {
                    rewardRecord = new WorldBossKillRewardRecord();
                }

                // Save level infos;
                raiderState.LatestBossLevel = bossState.Level;
                if (!rewardRecord.ContainsKey(raiderState.LatestBossLevel))
                {
                    rewardRecord.Add(raiderState.LatestBossLevel, false);
                }
                states = states.SetLegacyState(worldBossKillRewardRecordAddress, rewardRecord.Serialize());
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressHex}Raid Total Executed Time: {Elapsed}", addressHex, ended - started);
            return states
                .SetAvatarState(AvatarAddress, avatarState, true, true, false, false)
                .SetLegacyState(worldBossAddress, bossState.Serialize())
                .SetLegacyState(raiderAddress, raiderState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
                {
                    ["a"] = AvatarAddress.Serialize(),
                    ["e"] = new List(EquipmentIds.Select(e => e.Serialize())),
                    ["c"] = new List(CostumeIds.Select(c => c.Serialize())),
                    ["f"] = new List(FoodIds.Select(f => f.Serialize())),
                    ["r"] = RuneInfos.OrderBy(x => x.SlotIndex).Select(x=> x.Serialize()).Serialize(),
                    ["p"] = PayNcg.Serialize(),
                }
                .ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            EquipmentIds = plainValue["e"].ToList(StateExtensions.ToGuid);
            CostumeIds = plainValue["c"].ToList(StateExtensions.ToGuid);
            FoodIds = plainValue["f"].ToList(StateExtensions.ToGuid);
            RuneInfos = plainValue["r"].ToList(x => new RuneSlotInfo((List)x));
            PayNcg = plainValue["p"].ToBoolean();
        }
    }
}
