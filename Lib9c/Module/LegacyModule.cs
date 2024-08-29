using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using LruCacheNet;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
using Nekoyume.Model.Coupons;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stake;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Rune;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Module
{
    public static class LegacyModule
    {
        private const int SheetsCacheSize = 100;
        private static readonly LruCache<string, ISheet> SheetsCache =
            new LruCache<string, ISheet>(SheetsCacheSize);

        public static IValue GetLegacyState(this IWorldState worldState, Address address) =>
            worldState.GetAccountState(ReservedAddresses.LegacyAccount).GetState(address);

#nullable enable
        public static IReadOnlyList<IValue?> GetLegacyStates(this IWorldState worldState, IReadOnlyList<Address> addresses) =>
            worldState.GetAccountState(ReservedAddresses.LegacyAccount).GetStates(addresses);
#nullable disable

        public static IWorld SetLegacyState(this IWorld world, Address address, IValue state) =>
            world.SetAccount(
                ReservedAddresses.LegacyAccount,
                world.GetAccount(ReservedAddresses.LegacyAccount).SetState(address, state));

        public static IWorld RemoveLegacyState(this IWorld world, Address address) =>
            world.MutateAccount(
                ReservedAddresses.LegacyAccount,
                account => account.RemoveState(address));

        // Methods from AccountExtensions
        public static IWorld MarkBalanceChanged(
            this IWorld world,
            IActionContext context,
            Currency currency,
            params Address[] accounts
        )
        {
            if (accounts.Length == 1)
            {
                return world.MintAsset(context, accounts[0], currency * 1);
            }
            else if (accounts.Length < 1)
            {
                return world;
            }

            for (int i = 1; i < accounts.Length; i++)
            {
                world = world.TransferAsset(
                    context,
                    accounts[i - 1],
                    accounts[i],
                    currency * 1);
            }

            return world;
        }

        public static IWorld SetWorldBossKillReward(
            this IWorld world,
            IActionContext context,
            Address rewardInfoAddress,
            WorldBossKillRewardRecord rewardRecord,
            int rank,
            WorldBossState bossState,
            RuneWeightSheet runeWeightSheet,
            WorldBossKillRewardSheet worldBossKillRewardSheet,
            RuneSheet runeSheet,
            MaterialItemSheet materialItemSheet,
            IRandom random,
            Inventory inventory,
            Address avatarAddress,
            Address agentAddress)
        {
            if (!rewardRecord.IsClaimable(bossState.Level))
            {
                throw new InvalidClaimException();
            }
#pragma warning disable LAA1002
            var filtered = rewardRecord
                .Where(kv => !kv.Value)
                .Select(kv => kv.Key)
                .ToList();
#pragma warning restore LAA1002
            foreach (var level in filtered)
            {
                var rewards = WorldBossHelper.CalculateReward(
                    rank,
                    bossState.Id,
                    runeWeightSheet,
                    worldBossKillRewardSheet,
                    runeSheet,
                    materialItemSheet,
                    random
                );
                rewardRecord[level] = true;
                foreach (var reward in rewards.assets)
                {
                    if (reward.Currency.Equals(CrystalCalculator.CRYSTAL))
                    {
                        world = world.MintAsset(context, agentAddress, reward);
                    }
                    else
                    {
                        world = world.MintAsset(context, avatarAddress, reward);
                    }
                }

#pragma warning disable LAA1002
                foreach (var reward in rewards.materials)
#pragma warning restore LAA1002
                {
                    inventory.AddItem(reward.Key, reward.Value);
                }
            }

            return world
                .SetLegacyState(rewardInfoAddress, rewardRecord.Serialize())
                .SetInventory(avatarAddress, inventory);
        }

#nullable enable
        public static IWorld SetCouponWallet(
            this IWorld world,
            Address agentAddress,
            IImmutableDictionary<Guid, Coupon> couponWallet)
        {
            Address walletAddress = agentAddress.Derive(CouponWalletKey);
            IValue serializedWallet = new Bencodex.Types.List(
                couponWallet.Values.OrderBy(c => c.Id).Select(v => v.Serialize())
            );
            return SetLegacyState(world, walletAddress, serializedWallet);
        }
#nullable disable

        public static IWorld Mead(
            this IWorld world, IActionContext context, Address signer, BigInteger rawValue)
        {
            while (true)
            {
                var price = rawValue * Currencies.Mead;
                var balance = world.GetBalance(signer, Currencies.Mead);
                if (balance < price)
                {
                    var requiredMead = price - balance;
                    var contractAddress = signer.Derive(nameof(RequestPledge));
                    if (GetLegacyState(world, contractAddress) is List contract && contract[1].ToBoolean())
                    {
                        var patron = contract[0].ToAddress();
                        try
                        {
                            world = world.TransferAsset(context, patron, signer, requiredMead);
                        }
                        catch (InsufficientBalanceException)
                        {
                            world = Mead(world, context, patron, rawValue);
                            continue;
                        }
                    }
                    else
                    {
                        throw new InsufficientBalanceException("", signer, balance);
                    }
                }

                return world;
            }
        }

        // Methods from AccountStateExtensions
        public static bool TryGetLegacyState<T>(this IWorldState worldState, Address address, out T result)
            where T : IValue
        {
            IValue raw = GetLegacyState(worldState, address);
            if (raw is T v)
            {
                result = v;
                return true;
            }

            result = default;
            return false;
        }

        public static Dictionary<Address, IValue> GetLegacyStatesAsDict(this IWorldState worldState, params Address[] addresses)
        {
            var result = new Dictionary<Address, IValue>();
            var values = GetLegacyStates(worldState, addresses);
            for (var i = 0; i < addresses.Length; i++)
            {
                var address = addresses[i];
                var value = values[i];
                result[address] = value ?? Null.Value;
            }

            return result;
        }

        public static bool TryGetGoldBalance(
            this IWorldState worldState,
            Address address,
            Currency currency,
            out FungibleAssetValue balance)
        {
            try
            {
                balance = worldState.GetBalance(address, currency);
                return true;
            }
            catch (BalanceDoesNotExistsException)
            {
                balance = default;
                return false;
            }
        }

        public static GoldBalanceState GetGoldBalanceState(
            this IWorldState worldState,
            Address address,
            Currency currency
        ) => new GoldBalanceState(address, worldState.GetBalance(address, currency));

        public static Currency GetGoldCurrency(this IWorldState worldState)
        {
            if (TryGetLegacyState(worldState, GoldCurrencyState.Address, out Dictionary asDict))
            {
                return new GoldCurrencyState(asDict).Currency;
            }

            throw new InvalidOperationException(
                "The states doesn't contain gold currency.\n" +
                "Check the genesis block."
            );
        }

        public static WeeklyArenaState GetWeeklyArenaState(this IWorldState worldState, Address address)
        {
            var iValue = GetLegacyState(worldState, address);
            if (iValue is null)
            {
                Log.Warning("No weekly arena state ({0})", address.ToHex());
                return null;
            }

            try
            {
                return new WeeklyArenaState(iValue);
            }
            catch (InvalidCastException e)
            {
                Log.Error(
                    e,
                    "Invalid weekly arena state ({0}): {1}",
                    address.ToHex(),
                    iValue
                );

                return null;
            }
        }

        public static WeeklyArenaState GetWeeklyArenaState(this IWorldState worldState, int index)
        {
            var address = WeeklyArenaState.DeriveAddress(index);
            return GetWeeklyArenaState(worldState, address);
        }

        [Obsolete("Use AllCombinationSlotState.GetRuneState() instead.")]
        public static CombinationSlotState GetCombinationSlotStateLegacy(
            this IWorldState worldState,
            Address avatarAddress,
            int index)
        {
            var address = avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    index
                )
            );
            var value = GetLegacyState(worldState, address);
            if (value is null)
            {
                Log.Warning("No combination slot state ({0})", address.ToHex());
                return null;
            }

            try
            {
                return new CombinationSlotState((Dictionary)value);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Unexpected error occurred during {nameof(GetCombinationSlotStateLegacy)}()");
                throw;
            }
        }

        public static GameConfigState GetGameConfigState(this IWorldState worldState)
        {
            var value = GetLegacyState(worldState, GameConfigState.Address);
            if (value is null)
            {
                Log.Warning("No game config state ({0})", GameConfigState.Address.ToHex());
                return null;
            }

            try
            {
                return new GameConfigState((Dictionary)value);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Unexpected error occurred during {nameof(GetGameConfigState)}()");
                throw;
            }
        }

        public static RedeemCodeState GetRedeemCodeState(this IWorldState worldState)
        {
            var value = GetLegacyState(worldState, RedeemCodeState.Address);
            if (value is null)
            {
                Log.Warning("RedeemCodeState is null. ({0})", RedeemCodeState.Address.ToHex());
                return null;
            }

            try
            {
                return new RedeemCodeState((Dictionary)value);
            }
            catch (Exception e)
            {
                Log.Error(e, $"Unexpected error occurred during {nameof(GetRedeemCodeState)}()");
                throw;
            }
        }

#nullable enable
        public static IImmutableDictionary<Guid, Coupon> GetCouponWallet(this IWorldState worldState, Address agentAddress)
        {
            Address walletAddress = agentAddress.Derive(CouponWalletKey);
            IValue? serialized = GetLegacyState(worldState, walletAddress);
            if (!(serialized is { } serializedValue))
            {
                return ImmutableDictionary<Guid, Coupon>.Empty;
            }

            var serializedWallet = (Bencodex.Types.List)serializedValue;
            return serializedWallet
                .Select(serializedCoupon => new Coupon(serializedCoupon))
                .ToImmutableDictionary(v => v.Id, v => v);
        }
#nullable disable

        public static IEnumerable<GoldDistribution> GetGoldDistribution(this IWorldState worldState)
        {
            var value = GetLegacyState(worldState, Addresses.GoldDistribution);
            if (value is null)
            {
                Log.Warning($"{nameof(GoldDistribution)} is null ({0})", Addresses.GoldDistribution.ToHex());
                return null;
            }

            try
            {
                var goldDistributions = (Bencodex.Types.List)value;
                return goldDistributions.Select(v => new GoldDistribution(v));
            }
            catch (Exception e)
            {
                Log.Error(e, $"Unexpected error occurred during {nameof(GetGoldDistribution)}()");
                throw;
            }
        }

        public static T GetSheet<T>(this IWorldState worldState) where T : ISheet, new()
        {
            var address = Addresses.GetSheetAddress<T>();
            return GetSheet<T>(worldState, address);
        }

        public static T GetSheet<T>(
            this IWorldState worldState,
            Address sheetAddr)
            where T : ISheet, new()
        {
            try
            {
                var csv = GetSheetCsv<T>(worldState);
                byte[] hash;
                using (var sha256 = SHA256.Create())
                {
                    hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(csv));
                }

                var cacheKey = sheetAddr.ToHex() + ByteUtil.Hex(hash);
                if (SheetsCache.TryGetValue(cacheKey, out var cached))
                {
                    return (T)cached;
                }

                var sheet = new T();
                sheet.Set(csv);
                SheetsCache.AddOrUpdate(cacheKey, sheet);
                return sheet;
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected error occurred during GetSheet<{TypeName}>()", typeof(T).FullName);
                throw;
            }
        }

        public static bool TryGetSheet<T>(this IWorldState worldState, out T sheet) where T : ISheet, new()
        {
            try
            {
                sheet = GetSheet<T>(worldState);
                return true;
            }
            catch (Exception)
            {
                sheet = default;
                return false;
            }
        }

        public static bool TryGetSheet<T>(this IWorldState worldState, Address address, out T sheet)
            where T : ISheet, new()
        {
            try
            {
                sheet = GetSheet<T>(worldState, address);
                return true;
            }
            catch (Exception)
            {
                sheet = default;
                return false;
            }
        }

        public static Dictionary<Type, (Address address, ISheet sheet)> GetSheets(
            this IWorldState worldState,
            bool containAvatarSheets = false,
            bool containItemSheet = false,
            bool containQuestSheet = false,
            bool containSimulatorSheets = false,
            bool containStageSimulatorSheets = false,
            bool containRankingSimulatorSheets = false,
            bool containArenaSimulatorSheets = false,
            bool containValidateItemRequirementSheets = false,
            bool containRaidSimulatorSheets = false,
            IEnumerable<Type> sheetTypes = null)
        {
            var sheetTypeList = sheetTypes?.ToList() ?? new List<Type>();
            if (containAvatarSheets)
            {
                // AvatarSheets need QuestSheet
                containQuestSheet = true;
                sheetTypeList.Add(typeof(WorldSheet));
                sheetTypeList.Add(typeof(QuestRewardSheet));
                sheetTypeList.Add(typeof(QuestItemRewardSheet));
                sheetTypeList.Add(typeof(EquipmentItemRecipeSheet));
                sheetTypeList.Add(typeof(EquipmentItemSubRecipeSheet));
            }

            if (containItemSheet)
            {
                sheetTypeList.Add(typeof(ConsumableItemSheet));
                sheetTypeList.Add(typeof(CostumeItemSheet));
                sheetTypeList.Add(typeof(EquipmentItemSheet));
                sheetTypeList.Add(typeof(MaterialItemSheet));
            }

            if (containQuestSheet)
            {
                sheetTypeList.Add(typeof(WorldQuestSheet));
                sheetTypeList.Add(typeof(CollectQuestSheet));
                sheetTypeList.Add(typeof(CombinationQuestSheet));
                sheetTypeList.Add(typeof(TradeQuestSheet));
                sheetTypeList.Add(typeof(MonsterQuestSheet));
                sheetTypeList.Add(typeof(ItemEnhancementQuestSheet));
                sheetTypeList.Add(typeof(GeneralQuestSheet));
                sheetTypeList.Add(typeof(ItemGradeQuestSheet));
                sheetTypeList.Add(typeof(ItemTypeCollectQuestSheet));
                sheetTypeList.Add(typeof(GoldQuestSheet));
                sheetTypeList.Add(typeof(CombinationEquipmentQuestSheet));
            }

            if (containSimulatorSheets)
            {
                sheetTypeList.Add(typeof(MaterialItemSheet));
                sheetTypeList.Add(typeof(SkillSheet));
                sheetTypeList.Add(typeof(SkillBuffSheet));
                sheetTypeList.Add(typeof(StatBuffSheet));
                sheetTypeList.Add(typeof(SkillActionBuffSheet));
                sheetTypeList.Add(typeof(ActionBuffSheet));
                sheetTypeList.Add(typeof(CharacterSheet));
                sheetTypeList.Add(typeof(CharacterLevelSheet));
                sheetTypeList.Add(typeof(EquipmentItemSetEffectSheet));
                sheetTypeList.Add(typeof(RuneOptionSheet));
            }

            if (containStageSimulatorSheets)
            {
                sheetTypeList.Add(typeof(MaterialItemSheet));
                sheetTypeList.Add(typeof(SkillSheet));
                sheetTypeList.Add(typeof(SkillBuffSheet));
                sheetTypeList.Add(typeof(StatBuffSheet));
                sheetTypeList.Add(typeof(SkillActionBuffSheet));
                sheetTypeList.Add(typeof(ActionBuffSheet));
                sheetTypeList.Add(typeof(CharacterSheet));
                sheetTypeList.Add(typeof(CharacterLevelSheet));
                sheetTypeList.Add(typeof(EquipmentItemSetEffectSheet));
                sheetTypeList.Add(typeof(StageSheet));
                sheetTypeList.Add(typeof(StageWaveSheet));
                sheetTypeList.Add(typeof(EnemySkillSheet));
                sheetTypeList.Add(typeof(RuneOptionSheet));
            }

            if (containRankingSimulatorSheets)
            {
                sheetTypeList.Add(typeof(MaterialItemSheet));
                sheetTypeList.Add(typeof(SkillSheet));
                sheetTypeList.Add(typeof(SkillBuffSheet));
                sheetTypeList.Add(typeof(StatBuffSheet));
                sheetTypeList.Add(typeof(SkillActionBuffSheet));
                sheetTypeList.Add(typeof(ActionBuffSheet));
                sheetTypeList.Add(typeof(CharacterSheet));
                sheetTypeList.Add(typeof(CharacterLevelSheet));
                sheetTypeList.Add(typeof(EquipmentItemSetEffectSheet));
                sheetTypeList.Add(typeof(WeeklyArenaRewardSheet));
                sheetTypeList.Add(typeof(RuneOptionSheet));
            }

            if (containArenaSimulatorSheets)
            {
                sheetTypeList.Add(typeof(MaterialItemSheet));
                sheetTypeList.Add(typeof(SkillSheet));
                sheetTypeList.Add(typeof(SkillBuffSheet));
                sheetTypeList.Add(typeof(StatBuffSheet));
                sheetTypeList.Add(typeof(SkillActionBuffSheet));
                sheetTypeList.Add(typeof(ActionBuffSheet));
                sheetTypeList.Add(typeof(CharacterSheet));
                sheetTypeList.Add(typeof(CharacterLevelSheet));
                sheetTypeList.Add(typeof(EquipmentItemSetEffectSheet));
                sheetTypeList.Add(typeof(WeeklyArenaRewardSheet));
                sheetTypeList.Add(typeof(CostumeStatSheet));
                sheetTypeList.Add(typeof(RuneOptionSheet));
            }

            if (containValidateItemRequirementSheets)
            {
                sheetTypeList.Add(typeof(ItemRequirementSheet));
                sheetTypeList.Add(typeof(EquipmentItemRecipeSheet));
                sheetTypeList.Add(typeof(EquipmentItemSubRecipeSheetV2));
                sheetTypeList.Add(typeof(EquipmentItemOptionSheet));
            }

            if (containRaidSimulatorSheets)
            {
                sheetTypeList.Add(typeof(MaterialItemSheet));
                sheetTypeList.Add(typeof(SkillSheet));
                sheetTypeList.Add(typeof(SkillBuffSheet));
                sheetTypeList.Add(typeof(StatBuffSheet));
                sheetTypeList.Add(typeof(SkillActionBuffSheet));
                sheetTypeList.Add(typeof(ActionBuffSheet));
                sheetTypeList.Add(typeof(CharacterSheet));
                sheetTypeList.Add(typeof(CharacterLevelSheet));
                sheetTypeList.Add(typeof(EquipmentItemSetEffectSheet));
                sheetTypeList.Add(typeof(WorldBossCharacterSheet));
                sheetTypeList.Add(typeof(EnemySkillSheet));
                sheetTypeList.Add(typeof(WorldBossBattleRewardSheet));
                sheetTypeList.Add(typeof(RuneWeightSheet));
                sheetTypeList.Add(typeof(RuneSheet));
                sheetTypeList.Add(typeof(RuneOptionSheet));
            }

            return GetSheets(worldState, sheetTypeList.Distinct().ToArray());
        }

        public static Dictionary<Type, (Address address, ISheet sheet)> GetSheets(
            this IWorldState worldState,
            params Type[] sheetTypes)
        {
            Dictionary<Type, (Address address, ISheet sheet)> result = sheetTypes.ToDictionary(
                sheetType => sheetType,
                sheetType => (Addresses.GetSheetAddress(sheetType.Name), (ISheet)null));
            return GetSheetsInternal(worldState, result);
        }

        public static Dictionary<Type, (Address address, ISheet sheet)> GetSheets(
            this IWorldState worldState,
            params (Type sheetType, string sheetName)[] sheetTuples)
        {
            Dictionary<Type, (Address address, ISheet sheet)> result = sheetTuples.ToDictionary(
                tuple => tuple.sheetType,
                tuple => (Addresses.GetSheetAddress(tuple.sheetName), (ISheet)null));
            return GetSheetsInternal(worldState, result);
        }

        private static Dictionary<Type, (Address address, ISheet sheet)> GetSheetsInternal(
            this IWorldState worldState,
            Dictionary<Type, (Address address, ISheet sheet)> result)
        {
            var sheetTypes = result.Keys.ToArray();
            var addresses = result.Values.Select(e => e.address).ToArray();
            var csvValues = GetLegacyStates(worldState, addresses);
            for (var i = 0; i < sheetTypes.Length; i++)
            {
                var sheetType = sheetTypes[i];
                var address = addresses[i];
                var csvValue = csvValues[i];
                if (csvValue is null or Null)
                {
                    throw new FailedLoadStateException(address, sheetType);
                }

                var csv = csvValue.ToDotnetString();
                byte[] hash;
                using (var sha256 = SHA256.Create())
                {
                    hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(csv));
                }

                var cacheKey = address.ToHex() + ByteUtil.Hex(hash);
                if (SheetsCache.TryGetValue(cacheKey, out var cached))
                {
                    result[sheetType] = (address, cached);
                    continue;
                }

                var sheetConstructorInfo = sheetType.GetConstructor(Type.EmptyTypes);
                if (sheetConstructorInfo?.Invoke(Array.Empty<object>()) is not ISheet sheet)
                {
                    throw new FailedLoadSheetException(sheetType);
                }

                sheet.Set(csv);
                SheetsCache.AddOrUpdate(cacheKey, sheet);
                result[sheetType] = (address, sheet);
            }

            return result;
        }

        public static string GetSheetCsv<T>(this IWorldState worldState) where T : ISheet, new()
        {
            var address = Addresses.GetSheetAddress<T>();
            return LegacyModule.GetSheetCsv(worldState, address);
        }

        public static string GetSheetCsv(this IWorldState worldState, Address address)
        {
            var value = LegacyModule.GetLegacyState(worldState, address);
            if (value is null or Null)
            {
                throw new FailedLoadStateException(address, typeof(ISheet));
            }

            try
            {
                return value.ToDotnetString();
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected error occurred during GetSheetCsv({Address})", address);
                throw;
            }
        }

        public static ItemSheet GetItemSheet(this IWorldState worldState)
        {
            var sheet = new ItemSheet();
            sheet.Set(GetSheet<ConsumableItemSheet>(worldState), false);
            sheet.Set(GetSheet<CostumeItemSheet>(worldState), false);
            sheet.Set(GetSheet<EquipmentItemSheet>(worldState), false);
            sheet.Set(GetSheet<MaterialItemSheet>(worldState));
            return sheet;
        }

        public static StageSimulatorSheetsV1 GetStageSimulatorSheetsV1(this IWorldState worldState)
        {
            return new StageSimulatorSheetsV1(
                GetSheet<MaterialItemSheet>(worldState),
                GetSheet<SkillSheet>(worldState),
                GetSheet<SkillBuffSheet>(worldState),
                GetSheet<StatBuffSheet>(worldState),
                GetSheet<SkillActionBuffSheet>(worldState),
                GetSheet<ActionBuffSheet>(worldState),
                GetSheet<CharacterSheet>(worldState),
                GetSheet<CharacterLevelSheet>(worldState),
                GetSheet<EquipmentItemSetEffectSheet>(worldState),
                GetSheet<StageSheet>(worldState),
                GetSheet<StageWaveSheet>(worldState),
                GetSheet<EnemySkillSheet>(worldState)
            );
        }

        public static StageSimulatorSheets GetStageSimulatorSheets(this IWorldState worldState)
        {
            return new StageSimulatorSheets(
                GetSheet<MaterialItemSheet>(worldState),
                GetSheet<SkillSheet>(worldState),
                GetSheet<SkillBuffSheet>(worldState),
                GetSheet<StatBuffSheet>(worldState),
                GetSheet<SkillActionBuffSheet>(worldState),
                GetSheet<ActionBuffSheet>(worldState),
                GetSheet<CharacterSheet>(worldState),
                GetSheet<CharacterLevelSheet>(worldState),
                GetSheet<EquipmentItemSetEffectSheet>(worldState),
                GetSheet<StageSheet>(worldState),
                GetSheet<StageWaveSheet>(worldState),
                GetSheet<EnemySkillSheet>(worldState),
                GetSheet<RuneOptionSheet>(worldState),
                GetSheet<RuneListSheet>(worldState),
                GetSheet<RuneLevelBonusSheet>(worldState)
            );
        }

        public static RankingSimulatorSheetsV1 GetRankingSimulatorSheetsV1(this IWorldState worldState)
        {
            return new RankingSimulatorSheetsV1(
                GetSheet<MaterialItemSheet>(worldState),
                GetSheet<SkillSheet>(worldState),
                GetSheet<SkillBuffSheet>(worldState),
                GetSheet<StatBuffSheet>(worldState),
                GetSheet<SkillActionBuffSheet>(worldState),
                GetSheet<ActionBuffSheet>(worldState),
                GetSheet<CharacterSheet>(worldState),
                GetSheet<CharacterLevelSheet>(worldState),
                GetSheet<EquipmentItemSetEffectSheet>(worldState),
                GetSheet<WeeklyArenaRewardSheet>(worldState)
            );
        }

        public static RankingSimulatorSheets GetRankingSimulatorSheets(this IWorldState worldState)
        {
            return new RankingSimulatorSheets(
                GetSheet<MaterialItemSheet>(worldState),
                GetSheet<SkillSheet>(worldState),
                GetSheet<SkillBuffSheet>(worldState),
                GetSheet<StatBuffSheet>(worldState),
                GetSheet<SkillActionBuffSheet>(worldState),
                GetSheet<ActionBuffSheet>(worldState),
                GetSheet<CharacterSheet>(worldState),
                GetSheet<CharacterLevelSheet>(worldState),
                GetSheet<EquipmentItemSetEffectSheet>(worldState),
                GetSheet<WeeklyArenaRewardSheet>(worldState),
                GetSheet<RuneOptionSheet>(worldState),
                GetSheet<RuneListSheet>(worldState),
                GetSheet<RuneLevelBonusSheet>(worldState)
            );
        }

        public static QuestSheet GetQuestSheet(this IWorldState worldState)
        {
            var questSheet = new QuestSheet();
            questSheet.Set(GetSheet<WorldQuestSheet>(worldState), false);
            questSheet.Set(GetSheet<CollectQuestSheet>(worldState), false);
            questSheet.Set(GetSheet<CombinationQuestSheet>(worldState), false);
            questSheet.Set(GetSheet<TradeQuestSheet>(worldState), false);
            questSheet.Set(GetSheet<MonsterQuestSheet>(worldState), false);
            questSheet.Set(GetSheet<ItemEnhancementQuestSheet>(worldState), false);
            questSheet.Set(GetSheet<GeneralQuestSheet>(worldState), false);
            questSheet.Set(GetSheet<ItemGradeQuestSheet>(worldState), false);
            questSheet.Set(GetSheet<ItemTypeCollectQuestSheet>(worldState), false);
            questSheet.Set(GetSheet<GoldQuestSheet>(worldState), false);
            questSheet.Set(GetSheet<CombinationEquipmentQuestSheet>(worldState));
            return questSheet;
        }

        public static AvatarSheets GetAvatarSheets(this IWorldState worldState)
        {
            return new AvatarSheets(
                GetSheet<WorldSheet>(worldState),
                GetQuestSheet(worldState),
                GetSheet<QuestRewardSheet>(worldState),
                GetSheet<QuestItemRewardSheet>(worldState),
                GetSheet<EquipmentItemRecipeSheet>(worldState),
                GetSheet<EquipmentItemSubRecipeSheet>(worldState)
            );
        }

        public static RankingState GetRankingState(this IWorldState worldState)
        {
            var value = GetLegacyState(worldState, Addresses.Ranking);
            if (value is null)
            {
                throw new FailedLoadStateException(nameof(RankingState0));
            }

            return new RankingState((Dictionary)value);
        }

        public static RankingState1 GetRankingState1(this IWorldState worldState)
        {
            var value = GetLegacyState(worldState, Addresses.Ranking);
            if (value is null)
            {
                throw new FailedLoadStateException(nameof(RankingState1));
            }

            return new RankingState1((Dictionary)value);
        }

        public static RankingState0 GetRankingState0(this IWorldState worldState)
        {
            var value = GetLegacyState(worldState, Addresses.Ranking);
            if (value is null)
            {
                throw new FailedLoadStateException(nameof(RankingState0));
            }

            return new RankingState0((Dictionary)value);
        }

        public static ShopState GetShopState(this IWorldState worldState)
        {
            var value = GetLegacyState(worldState, Addresses.Shop);
            if (value is null)
            {
                throw new FailedLoadStateException(nameof(ShopState));
            }

            return new ShopState((Dictionary)value);
        }

        public static (Address arenaInfoAddress, ArenaInfo arenaInfo, bool isNewArenaInfo) GetArenaInfo(
            this IWorldState worldState,
            Address weeklyArenaAddress,
            AvatarState avatarState,
            CharacterSheet characterSheet,
            CostumeStatSheet costumeStatSheet)
        {
            var arenaInfoAddress = weeklyArenaAddress.Derive(avatarState.address.ToByteArray());
            var isNew = false;
            ArenaInfo arenaInfo;
            if (TryGetLegacyState(worldState, arenaInfoAddress, out Dictionary rawArenaInfo))
            {
                arenaInfo = new ArenaInfo(rawArenaInfo);
            }
            else
            {
                arenaInfo = new ArenaInfo(avatarState, characterSheet, costumeStatSheet, true);
                isNew = true;
            }

            return (arenaInfoAddress, arenaInfo, isNew);
        }

        public static bool TryGetLegacyStakeState(
            this IWorldState worldState,
            Address agentAddress,
            out LegacyStakeState legacyStakeState)
        {
            if (TryGetLegacyState(worldState, LegacyStakeState.DeriveAddress(agentAddress), out Dictionary dictionary))
            {
                legacyStakeState = new LegacyStakeState(dictionary);
                return true;
            }

            legacyStakeState = null;
            return false;
        }

        public static FungibleAssetValue GetStakedAmount(
            this IWorldState worldState,
            Address agentAddr)
        {
            var goldCurrency = GetGoldCurrency(worldState);
            return worldState.GetBalance(LegacyStakeState.DeriveAddress(agentAddr), goldCurrency);
        }

        public static bool TryGetStakeState(
            this IWorldState worldState,
            Address agentAddr,
            out StakeState stakeState)
        {
            var stakeStateAddr = StakeState.DeriveAddress(agentAddr);
            return StakeStateUtils.TryMigrate(
                worldState,
                stakeStateAddr,
                out stakeState);
        }

        public static ArenaParticipants GetArenaParticipants(
            this IWorldState worldState, Address arenaParticipantsAddress, int id, int round)
        {
            return TryGetLegacyState(worldState, arenaParticipantsAddress, out List list)
                ? new ArenaParticipants(list)
                : new ArenaParticipants(id, round);
        }

        public static ArenaAvatarState GetArenaAvatarState(
            this IWorldState worldState,
            Address arenaAvatarStateAddress,
            AvatarState avatarState)
        {
            return TryGetLegacyState(worldState, arenaAvatarStateAddress, out List list)
                ? new ArenaAvatarState(list)
                : new ArenaAvatarState(avatarState);
        }

        public static bool TryGetArenaParticipants(
            this IWorldState worldState,
            Address arenaParticipantsAddress,
            out ArenaParticipants arenaParticipants)
        {
            if (TryGetLegacyState(worldState, arenaParticipantsAddress, out List list))
            {
                arenaParticipants = new ArenaParticipants(list);
                return true;
            }

            arenaParticipants = null;
            return false;
        }

        public static bool TryGetArenaAvatarState(
            this IWorldState worldState,
            Address arenaAvatarStateAddress,
            out ArenaAvatarState arenaAvatarState)
        {
            if (TryGetLegacyState(worldState, arenaAvatarStateAddress, out List list))
            {
                arenaAvatarState = new ArenaAvatarState(list);
                return true;
            }

            arenaAvatarState = null;
            return false;
        }

        public static bool TryGetArenaScore(
            this IWorldState worldState,
            Address arenaScoreAddress,
            out ArenaScore arenaScore)
        {
            if (TryGetLegacyState(worldState, arenaScoreAddress, out List list))
            {
                arenaScore = new ArenaScore(list);
                return true;
            }

            arenaScore = null;
            return false;
        }

        public static bool TryGetArenaInformation(
            this IWorldState worldState,
            Address arenaInformationAddress,
            out ArenaInformation arenaInformation)
        {
            if (TryGetLegacyState(worldState, arenaInformationAddress, out List list))
            {
                arenaInformation = new ArenaInformation(list);
                return true;
            }

            arenaInformation = null;
            return false;
        }

        public static CrystalCostState GetCrystalCostState(
            this IWorldState worldState,
            Address address)
        {
            return TryGetLegacyState(worldState, address, out List rawState)
                ? new CrystalCostState(address, rawState)
                : new CrystalCostState(address, 0 * CrystalCalculator.CRYSTAL);
        }

        public static (
            CrystalCostState DailyCostState,
            CrystalCostState WeeklyCostState,
            CrystalCostState PrevWeeklyCostState,
            CrystalCostState BeforePrevWeeklyCostState
            ) GetCrystalCostStates(this IWorldState worldState, long blockIndex, long interval)
        {
            int dailyCostIndex = (int)(blockIndex / CrystalCostState.DailyIntervalIndex);
            int weeklyCostIndex = (int)(blockIndex / interval);
            Address dailyCostAddress = Addresses.GetDailyCrystalCostAddress(dailyCostIndex);
            CrystalCostState dailyCostState = GetCrystalCostState(worldState, dailyCostAddress);
            Address weeklyCostAddress = Addresses.GetWeeklyCrystalCostAddress(weeklyCostIndex);
            CrystalCostState weeklyCostState = GetCrystalCostState(worldState, weeklyCostAddress);
            CrystalCostState prevWeeklyCostState = null;
            CrystalCostState beforePrevWeeklyCostState = null;
            if (weeklyCostIndex > 1)
            {
                Address prevWeeklyCostAddress =
                    Addresses.GetWeeklyCrystalCostAddress(weeklyCostIndex - 1);
                prevWeeklyCostState = GetCrystalCostState(worldState, prevWeeklyCostAddress);
                Address beforePrevWeeklyCostAddress =
                    Addresses.GetWeeklyCrystalCostAddress(weeklyCostIndex - 2);
                beforePrevWeeklyCostState = GetCrystalCostState(worldState, beforePrevWeeklyCostAddress);
            }

            return (dailyCostState, weeklyCostState, prevWeeklyCostState,
                beforePrevWeeklyCostState);
        }

        public static void ValidateWorldId(
            this IWorldState worldState,
            Address avatarAddress,
            int worldId)
        {
            if (worldId > 1)
            {
                if (worldId == GameConfig.MimisbrunnrWorldId)
                {
                    throw new InvalidWorldException();
                }

                var unlockedWorldIdsAddress = avatarAddress.Derive("world_ids");

                // Unlock First.
                if (!TryGetLegacyState(worldState, unlockedWorldIdsAddress, out List rawIds))
                {
                    throw new InvalidWorldException();
                }

                List<int> unlockedWorldIds = rawIds.ToList(StateExtensions.ToInteger);
                if (!unlockedWorldIds.Contains(worldId))
                {
                    throw new InvalidWorldException();
                }
            }
        }

        public static RaiderState GetRaiderState(
            this IWorldState worldState,
            Address avatarAddress,
            int raidId)
        {
            return GetRaiderState(worldState, Addresses.GetRaiderAddress(avatarAddress, raidId));
        }

        public static RaiderState GetRaiderState(
            this IWorldState worldState,
            Address raiderAddress)
        {
            if (TryGetLegacyState(worldState, raiderAddress, out List rawRaider))
            {
                return new RaiderState(rawRaider);
            }

            throw new FailedLoadStateException("can't find RaiderState.");
        }

        public static Dictionary<Type, (Address address, ISheet sheet)> GetSheetsV100291(
            this IWorldState worldState,
            bool containAvatarSheets = false,
            bool containItemSheet = false,
            bool containQuestSheet = false,
            bool containSimulatorSheets = false,
            bool containStageSimulatorSheets = false,
            bool containRankingSimulatorSheets = false,
            bool containArenaSimulatorSheets = false,
            bool containValidateItemRequirementSheets = false,
            IEnumerable<Type> sheetTypes = null)
        {
            var sheetTypeList = sheetTypes?.ToList() ?? new List<Type>();
            if (containAvatarSheets)
            {
                // AvatarSheets need QuestSheet
                containQuestSheet = true;
                sheetTypeList.Add(typeof(WorldSheet));
                sheetTypeList.Add(typeof(QuestRewardSheet));
                sheetTypeList.Add(typeof(QuestItemRewardSheet));
                sheetTypeList.Add(typeof(EquipmentItemRecipeSheet));
                sheetTypeList.Add(typeof(EquipmentItemSubRecipeSheet));
            }

            if (containItemSheet)
            {
                sheetTypeList.Add(typeof(ConsumableItemSheet));
                sheetTypeList.Add(typeof(CostumeItemSheet));
                sheetTypeList.Add(typeof(EquipmentItemSheet));
                sheetTypeList.Add(typeof(MaterialItemSheet));
            }

            if (containQuestSheet)
            {
                sheetTypeList.Add(typeof(WorldQuestSheet));
                sheetTypeList.Add(typeof(CollectQuestSheet));
                sheetTypeList.Add(typeof(CombinationQuestSheet));
                sheetTypeList.Add(typeof(TradeQuestSheet));
                sheetTypeList.Add(typeof(MonsterQuestSheet));
                sheetTypeList.Add(typeof(ItemEnhancementQuestSheet));
                sheetTypeList.Add(typeof(GeneralQuestSheet));
                sheetTypeList.Add(typeof(ItemGradeQuestSheet));
                sheetTypeList.Add(typeof(ItemTypeCollectQuestSheet));
                sheetTypeList.Add(typeof(GoldQuestSheet));
                sheetTypeList.Add(typeof(CombinationEquipmentQuestSheet));
            }

            if (containSimulatorSheets)
            {
                sheetTypeList.Add(typeof(MaterialItemSheet));
                sheetTypeList.Add(typeof(SkillSheet));
                sheetTypeList.Add(typeof(SkillBuffSheet));
                sheetTypeList.Add(typeof(BuffSheet));
                sheetTypeList.Add(typeof(CharacterSheet));
                sheetTypeList.Add(typeof(CharacterLevelSheet));
                sheetTypeList.Add(typeof(EquipmentItemSetEffectSheet));
            }

            if (containStageSimulatorSheets)
            {
                sheetTypeList.Add(typeof(MaterialItemSheet));
                sheetTypeList.Add(typeof(SkillSheet));
                sheetTypeList.Add(typeof(SkillBuffSheet));
                sheetTypeList.Add(typeof(BuffSheet));
                sheetTypeList.Add(typeof(CharacterSheet));
                sheetTypeList.Add(typeof(CharacterLevelSheet));
                sheetTypeList.Add(typeof(EquipmentItemSetEffectSheet));
                sheetTypeList.Add(typeof(StageSheet));
                sheetTypeList.Add(typeof(StageWaveSheet));
                sheetTypeList.Add(typeof(EnemySkillSheet));
            }

            if (containRankingSimulatorSheets)
            {
                sheetTypeList.Add(typeof(MaterialItemSheet));
                sheetTypeList.Add(typeof(SkillSheet));
                sheetTypeList.Add(typeof(SkillBuffSheet));
                sheetTypeList.Add(typeof(BuffSheet));
                sheetTypeList.Add(typeof(CharacterSheet));
                sheetTypeList.Add(typeof(CharacterLevelSheet));
                sheetTypeList.Add(typeof(EquipmentItemSetEffectSheet));
                sheetTypeList.Add(typeof(WeeklyArenaRewardSheet));
            }

            if (containArenaSimulatorSheets)
            {
                sheetTypeList.Add(typeof(MaterialItemSheet));
                sheetTypeList.Add(typeof(SkillSheet));
                sheetTypeList.Add(typeof(SkillBuffSheet));
                sheetTypeList.Add(typeof(BuffSheet));
                sheetTypeList.Add(typeof(CharacterSheet));
                sheetTypeList.Add(typeof(CharacterLevelSheet));
                sheetTypeList.Add(typeof(EquipmentItemSetEffectSheet));
                sheetTypeList.Add(typeof(WeeklyArenaRewardSheet));
                sheetTypeList.Add(typeof(CostumeStatSheet));
            }

            if (containValidateItemRequirementSheets)
            {
                sheetTypeList.Add(typeof(ItemRequirementSheet));
                sheetTypeList.Add(typeof(EquipmentItemRecipeSheet));
                sheetTypeList.Add(typeof(EquipmentItemSubRecipeSheetV2));
                sheetTypeList.Add(typeof(EquipmentItemOptionSheet));
            }

            return GetSheets(worldState, sheetTypeList.Distinct().ToArray());
        }

        public static Dictionary<Type, (Address address, ISheet sheet)> GetSheetsV1(
            this IWorldState worldState,
            bool containAvatarSheets = false,
            bool containItemSheet = false,
            bool containQuestSheet = false,
            bool containSimulatorSheets = false,
            bool containStageSimulatorSheets = false,
            bool containRankingSimulatorSheets = false,
            bool containArenaSimulatorSheets = false,
            bool containValidateItemRequirementSheets = false,
            bool containRaidSimulatorSheets = false,
            IEnumerable<Type> sheetTypes = null)
        {
            var sheetTypeList = sheetTypes?.ToList() ?? new List<Type>();
            if (containAvatarSheets)
            {
                // AvatarSheets need QuestSheet
                containQuestSheet = true;
                sheetTypeList.Add(typeof(WorldSheet));
                sheetTypeList.Add(typeof(QuestRewardSheet));
                sheetTypeList.Add(typeof(QuestItemRewardSheet));
                sheetTypeList.Add(typeof(EquipmentItemRecipeSheet));
                sheetTypeList.Add(typeof(EquipmentItemSubRecipeSheet));
            }

            if (containItemSheet)
            {
                sheetTypeList.Add(typeof(ConsumableItemSheet));
                sheetTypeList.Add(typeof(CostumeItemSheet));
                sheetTypeList.Add(typeof(EquipmentItemSheet));
                sheetTypeList.Add(typeof(MaterialItemSheet));
            }

            if (containQuestSheet)
            {
                sheetTypeList.Add(typeof(WorldQuestSheet));
                sheetTypeList.Add(typeof(CollectQuestSheet));
                sheetTypeList.Add(typeof(CombinationQuestSheet));
                sheetTypeList.Add(typeof(TradeQuestSheet));
                sheetTypeList.Add(typeof(MonsterQuestSheet));
                sheetTypeList.Add(typeof(ItemEnhancementQuestSheet));
                sheetTypeList.Add(typeof(GeneralQuestSheet));
                sheetTypeList.Add(typeof(ItemGradeQuestSheet));
                sheetTypeList.Add(typeof(ItemTypeCollectQuestSheet));
                sheetTypeList.Add(typeof(GoldQuestSheet));
                sheetTypeList.Add(typeof(CombinationEquipmentQuestSheet));
            }

            if (containSimulatorSheets)
            {
                sheetTypeList.Add(typeof(MaterialItemSheet));
                sheetTypeList.Add(typeof(SkillSheet));
                sheetTypeList.Add(typeof(SkillBuffSheet));
                sheetTypeList.Add(typeof(StatBuffSheet));
                sheetTypeList.Add(typeof(SkillActionBuffSheet));
                sheetTypeList.Add(typeof(ActionBuffSheet));
                sheetTypeList.Add(typeof(CharacterSheet));
                sheetTypeList.Add(typeof(CharacterLevelSheet));
                sheetTypeList.Add(typeof(EquipmentItemSetEffectSheet));
            }

            if (containStageSimulatorSheets)
            {
                sheetTypeList.Add(typeof(MaterialItemSheet));
                sheetTypeList.Add(typeof(SkillSheet));
                sheetTypeList.Add(typeof(SkillBuffSheet));
                sheetTypeList.Add(typeof(StatBuffSheet));
                sheetTypeList.Add(typeof(SkillActionBuffSheet));
                sheetTypeList.Add(typeof(ActionBuffSheet));
                sheetTypeList.Add(typeof(CharacterSheet));
                sheetTypeList.Add(typeof(CharacterLevelSheet));
                sheetTypeList.Add(typeof(EquipmentItemSetEffectSheet));
                sheetTypeList.Add(typeof(StageSheet));
                sheetTypeList.Add(typeof(StageWaveSheet));
                sheetTypeList.Add(typeof(EnemySkillSheet));
            }

            if (containRankingSimulatorSheets)
            {
                sheetTypeList.Add(typeof(MaterialItemSheet));
                sheetTypeList.Add(typeof(SkillSheet));
                sheetTypeList.Add(typeof(SkillBuffSheet));
                sheetTypeList.Add(typeof(StatBuffSheet));
                sheetTypeList.Add(typeof(SkillActionBuffSheet));
                sheetTypeList.Add(typeof(ActionBuffSheet));
                sheetTypeList.Add(typeof(CharacterSheet));
                sheetTypeList.Add(typeof(CharacterLevelSheet));
                sheetTypeList.Add(typeof(EquipmentItemSetEffectSheet));
                sheetTypeList.Add(typeof(WeeklyArenaRewardSheet));
            }

            if (containArenaSimulatorSheets)
            {
                sheetTypeList.Add(typeof(MaterialItemSheet));
                sheetTypeList.Add(typeof(SkillSheet));
                sheetTypeList.Add(typeof(SkillBuffSheet));
                sheetTypeList.Add(typeof(StatBuffSheet));
                sheetTypeList.Add(typeof(SkillActionBuffSheet));
                sheetTypeList.Add(typeof(ActionBuffSheet));
                sheetTypeList.Add(typeof(CharacterSheet));
                sheetTypeList.Add(typeof(CharacterLevelSheet));
                sheetTypeList.Add(typeof(EquipmentItemSetEffectSheet));
                sheetTypeList.Add(typeof(WeeklyArenaRewardSheet));
                sheetTypeList.Add(typeof(CostumeStatSheet));
            }

            if (containValidateItemRequirementSheets)
            {
                sheetTypeList.Add(typeof(ItemRequirementSheet));
                sheetTypeList.Add(typeof(EquipmentItemRecipeSheet));
                sheetTypeList.Add(typeof(EquipmentItemSubRecipeSheetV2));
                sheetTypeList.Add(typeof(EquipmentItemOptionSheet));
            }

            if (containRaidSimulatorSheets)
            {
                sheetTypeList.Add(typeof(MaterialItemSheet));
                sheetTypeList.Add(typeof(SkillSheet));
                sheetTypeList.Add(typeof(SkillBuffSheet));
                sheetTypeList.Add(typeof(StatBuffSheet));
                sheetTypeList.Add(typeof(SkillActionBuffSheet));
                sheetTypeList.Add(typeof(ActionBuffSheet));
                sheetTypeList.Add(typeof(CharacterSheet));
                sheetTypeList.Add(typeof(CharacterLevelSheet));
                sheetTypeList.Add(typeof(EquipmentItemSetEffectSheet));
                sheetTypeList.Add(typeof(WorldBossCharacterSheet));
                sheetTypeList.Add(typeof(EnemySkillSheet));
                sheetTypeList.Add(typeof(WorldBossBattleRewardSheet));
                sheetTypeList.Add(typeof(RuneWeightSheet));
                sheetTypeList.Add(typeof(RuneSheet));
            }

            return GetSheets(worldState, sheetTypeList.Distinct().ToArray());
        }
    }
}
