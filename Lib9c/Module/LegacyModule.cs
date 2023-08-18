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
using Nekoyume.Action.Extensions;
using Nekoyume.Exceptions;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
using Nekoyume.Model.Coupons;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Module
{
    public static class LegacyModule
    {
        private const int SheetsCacheSize = 100;
        private static readonly LruCache<string, ISheet> SheetsCache =
            new LruCache<string, ISheet>(SheetsCacheSize);

        // Basic implementations from IAccount and IAccountState
        public static IValue GetState(IWorld world, Address address) =>
            world.GetAccount(ReservedAddresses.LegacyAccount).GetState(address);

#nullable enable
        public static IReadOnlyList<IValue?> GetStates(IWorld world, IReadOnlyList<Address> addresses) =>
            world.GetAccount(ReservedAddresses.LegacyAccount).GetStates(addresses);
#nullable disable

        public static IWorld SetState(IWorld world, Address address, IValue state) =>
            world.SetAccount(
                world.GetAccount(ReservedAddresses.LegacyAccount).SetState(address, state));

        public static FungibleAssetValue GetBalance(
            IWorld world,
            Address address,
            Currency currency) =>
            world.GetAccount(ReservedAddresses.LegacyAccount).GetBalance(address, currency);

        public static FungibleAssetValue GetTotalSupply(IWorld world, Currency currency) =>
            world.GetAccount(ReservedAddresses.LegacyAccount).GetTotalSupply(currency);

        public static IWorld MintAsset(
            IWorld world,
            IActionContext context,
            Address recipient,
            FungibleAssetValue value) =>
            world.SetAccount(
                world.GetAccount(ReservedAddresses.LegacyAccount)
                    .MintAsset(context, recipient, value));

        public static IWorld TransferAsset(
            IWorld world,
            IActionContext context,
            Address sender,
            Address recipient,
            FungibleAssetValue value,
            bool allowNegativeBalance = false) =>
            world.SetAccount(
            world.GetAccount(ReservedAddresses.LegacyAccount)
                .TransferAsset(context, sender, recipient, value, allowNegativeBalance));

        public static IWorld BurnAsset(
            IWorld world,
            IActionContext context,
            Address owner,
            FungibleAssetValue value) =>
            world.SetAccount(
                world.GetAccount(ReservedAddresses.LegacyAccount)
                    .BurnAsset(context, owner, value));

        public static IWorld SetValidator(
            IWorld world,
            Libplanet.Types.Consensus.Validator validator) =>
            world.SetAccount(
                world.GetAccount(ReservedAddresses.LegacyAccount)
                    .SetValidator(validator));

        // Methods from AccountExtensions
        public static IWorld MarkBalanceChanged(
            IWorld world,
            IActionContext context,
            Currency currency,
            params Address[] accounts
        )
        {
            if (accounts.Length == 1)
            {
                return MintAsset(world, context, accounts[0], currency * 1);
            }
            else if (accounts.Length < 1)
            {
                return world;
            }

            for (int i = 1; i < accounts.Length; i++)
            {
                world = TransferAsset(
                    world,
                    context,
                    accounts[i - 1],
                    accounts[i],
                    currency * 1,
                    true);
            }

            return world;
        }

        public static IWorld SetWorldBossKillReward(
            IWorld world,
            IActionContext context,
            Address rewardInfoAddress,
            WorldBossKillRewardRecord rewardRecord,
            int rank,
            WorldBossState bossState,
            RuneWeightSheet runeWeightSheet,
            WorldBossKillRewardSheet worldBossKillRewardSheet,
            RuneSheet runeSheet,
            IRandom random,
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
                List<FungibleAssetValue> rewards = RuneHelper.CalculateReward(
                    rank,
                    bossState.Id,
                    runeWeightSheet,
                    worldBossKillRewardSheet,
                    runeSheet,
                    random
                );
                rewardRecord[level] = true;
                foreach (var reward in rewards)
                {
                    if (reward.Currency.Equals(CrystalCalculator.CRYSTAL))
                    {
                        world = MintAsset(world, context, agentAddress, reward);
                    }
                    else
                    {
                        world = MintAsset(world, context, avatarAddress, reward);
                    }
                }
            }

            return SetState(world, rewardInfoAddress, rewardRecord.Serialize());
        }

#nullable enable
        public static IWorld SetCouponWallet(
            IWorld world,
            Address agentAddress,
            IImmutableDictionary<Guid, Coupon> couponWallet,
            bool rehearsal = false)
        {
            Address walletAddress = agentAddress.Derive(CouponWalletKey);
            if (rehearsal)
            {
                return SetState(world, walletAddress, ActionBase.MarkChanged);
            }

            IValue serializedWallet = new Bencodex.Types.List(
                couponWallet.Values.OrderBy(c => c.Id).Select(v => v.Serialize())
            );
            return SetState(world, walletAddress, serializedWallet);
        }
#nullable disable

        public static IWorld Mead(
            IWorld world, IActionContext context, Address signer, BigInteger rawValue)
        {
            while (true)
            {
                var price = rawValue * Currencies.Mead;
                var balance = GetBalance(world, signer, Currencies.Mead);
                if (balance < price)
                {
                    var requiredMead = price - balance;
                    var contractAddress = signer.Derive(nameof(RequestPledge));
                    if (GetState(world, contractAddress) is List contract && contract[1].ToBoolean())
                    {
                        var patron = contract[0].ToAddress();
                        try
                        {
                            world = TransferAsset(world, context, patron, signer, requiredMead);
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
        public static bool TryGetState<T>(IWorld world, Address address, out T result)
            where T : IValue
        {
            IValue raw = GetState(world, address);
            if (raw is T v)
            {
                result = v;
                return true;
            }

            result = default;
            return false;
        }

        public static Dictionary<Address, IValue> GetStatesAsDict(IWorld world, params Address[] addresses)
        {
            var result = new Dictionary<Address, IValue>();
            var values = GetStates(world, addresses);
            for (var i = 0; i < addresses.Length; i++)
            {
                var address = addresses[i];
                var value = values[i];
                result[address] = value ?? Null.Value;
            }

            return result;
        }

        public static bool TryGetGoldBalance(
            IWorld world,
            Address address,
            Currency currency,
            out FungibleAssetValue balance)
        {
            try
            {
                balance = GetBalance(world, address, currency);
                return true;
            }
            catch (BalanceDoesNotExistsException)
            {
                balance = default;
                return false;
            }
        }

        public static GoldBalanceState GetGoldBalanceState(
            IWorld world,
            Address address,
            Currency currency
        ) => new GoldBalanceState(address, GetBalance(world, address, currency));

        public static Currency GetGoldCurrency(IWorld world)
        {
            if (TryGetState(world, GoldCurrencyState.Address, out Dictionary asDict))
            {
                return new GoldCurrencyState(asDict).Currency;
            }

            throw new InvalidOperationException(
                "The states doesn't contain gold currency.\n" +
                "Check the genesis block."
            );
        }

        public static WeeklyArenaState GetWeeklyArenaState(IWorld world, Address address)
        {
            var iValue = world.GetAccount(ReservedAddresses.LegacyAccount).GetState(address);
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

        public static WeeklyArenaState GetWeeklyArenaState(IWorld world, int index)
        {
            var address = WeeklyArenaState.DeriveAddress(index);
            return GetWeeklyArenaState(world, address);
        }

        public static CombinationSlotState GetCombinationSlotState(
            IWorld world,
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
            var value = GetState(world, address);
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
                Log.Error(e, $"Unexpected error occurred during {nameof(GetCombinationSlotState)}()");
                throw;
            }
        }

        public static GameConfigState GetGameConfigState(IWorld world)
        {
            var value = GetState(world, GameConfigState.Address);
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
                Log.Error(e, $"Unexpected error occurred during {nameof(GetCombinationSlotState)}()");
                throw;
            }
        }

        public static RedeemCodeState GetRedeemCodeState(IWorld world)
        {
            var value = GetState(world, RedeemCodeState.Address);
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
                Log.Error(e, $"Unexpected error occurred during {nameof(GetCombinationSlotState)}()");
                throw;
            }
        }

#nullable enable
        public static IImmutableDictionary<Guid, Coupon> GetCouponWallet(IWorld world, Address agentAddress)
        {
            Address walletAddress = agentAddress.Derive(CouponWalletKey);
            IValue? serialized = GetState(world, walletAddress);
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

        public static IEnumerable<GoldDistribution> GetGoldDistribution(IWorld world)
        {
            var value = GetState(world, Addresses.GoldDistribution);
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

        public static T GetSheet<T>(IWorld world) where T : ISheet, new()
        {
            var address = Addresses.GetSheetAddress<T>();

            try
            {
                var csv = GetSheetCsv<T>(world);
                byte[] hash;
                using (var sha256 = SHA256.Create())
                {
                    hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(csv));
                }

                var cacheKey = address.ToHex() + ByteUtil.Hex(hash);
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

        public static bool TryGetSheet<T>(IWorld world, out T sheet) where T : ISheet, new()
        {
            try
            {
                sheet = GetSheet<T>(world);
                return true;
            }
            catch (Exception)
            {
                sheet = default;
                return false;
            }
        }

        public static Dictionary<Type, (Address address, ISheet sheet)> GetSheets(
            IWorld world,
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

            return GetSheets(world, sheetTypeList.Distinct().ToArray());
        }

        public static Dictionary<Type, (Address address, ISheet sheet)> GetSheets(
            IWorld world,
            params Type[] sheetTypes)
        {
            Dictionary<Type, (Address address, ISheet sheet)> result = sheetTypes.ToDictionary(
                sheetType => sheetType,
                sheetType => (Addresses.GetSheetAddress(sheetType.Name), (ISheet)null));
#pragma warning disable LAA1002
            var addresses = result
                .Select(tuple => tuple.Value.address)
                .ToArray();
#pragma warning restore LAA1002
            var csvValues = GetStates(world, addresses);
            for (var i = 0; i < sheetTypes.Length; i++)
            {
                var sheetType = sheetTypes[i];
                var address = addresses[i];
                var csvValue = csvValues[i];
                if (csvValue is null)
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
                if (!(sheetConstructorInfo?.Invoke(Array.Empty<object>()) is ISheet sheet))
                {
                    throw new FailedLoadSheetException(sheetType);
                }

                sheet.Set(csv);
                SheetsCache.AddOrUpdate(cacheKey, sheet);
                result[sheetType] = (address, sheet);
            }

            return result;
        }

        public static string GetSheetCsv<T>(IWorld world) where T : ISheet, new()
        {
            var address = Addresses.GetSheetAddress<T>();
            var value = GetState(world, address);
            if (value is null)
            {
                Log.Warning("{TypeName} is null ({Address})", typeof(T).FullName, address.ToHex());
                throw new FailedLoadStateException(typeof(T).FullName);
            }

            try
            {
                return value.ToDotnetString();
            }
            catch (Exception e)
            {
                Log.Error(e, "Unexpected error occurred during GetSheetCsv<{TypeName}>()", typeof(T).FullName);
                throw;
            }
        }

        public static ItemSheet GetItemSheet(IWorld world)
        {
            var sheet = new ItemSheet();
            sheet.Set(GetSheet<ConsumableItemSheet>(world), false);
            sheet.Set(GetSheet<CostumeItemSheet>(world), false);
            sheet.Set(GetSheet<EquipmentItemSheet>(world), false);
            sheet.Set(GetSheet<MaterialItemSheet>(world));
            return sheet;
        }

        public static StageSimulatorSheetsV1 GetStageSimulatorSheetsV1(IWorld world)
        {
            return new StageSimulatorSheetsV1(
                GetSheet<MaterialItemSheet>(world),
                GetSheet<SkillSheet>(world),
                GetSheet<SkillBuffSheet>(world),
                GetSheet<StatBuffSheet>(world),
                GetSheet<SkillActionBuffSheet>(world),
                GetSheet<ActionBuffSheet>(world),
                GetSheet<CharacterSheet>(world),
                GetSheet<CharacterLevelSheet>(world),
                GetSheet<EquipmentItemSetEffectSheet>(world),
                GetSheet<StageSheet>(world),
                GetSheet<StageWaveSheet>(world),
                GetSheet<EnemySkillSheet>(world)
            );
        }

        public static StageSimulatorSheets GetStageSimulatorSheets(IWorld world)
        {
            return new StageSimulatorSheets(
                GetSheet<MaterialItemSheet>(world),
                GetSheet<SkillSheet>(world),
                GetSheet<SkillBuffSheet>(world),
                GetSheet<StatBuffSheet>(world),
                GetSheet<SkillActionBuffSheet>(world),
                GetSheet<ActionBuffSheet>(world),
                GetSheet<CharacterSheet>(world),
                GetSheet<CharacterLevelSheet>(world),
                GetSheet<EquipmentItemSetEffectSheet>(world),
                GetSheet<StageSheet>(world),
                GetSheet<StageWaveSheet>(world),
                GetSheet<EnemySkillSheet>(world),
                GetSheet<RuneOptionSheet>(world)
            );
        }

        public static RankingSimulatorSheetsV1 GetRankingSimulatorSheetsV1(IWorld world)
        {
            return new RankingSimulatorSheetsV1(
                GetSheet<MaterialItemSheet>(world),
                GetSheet<SkillSheet>(world),
                GetSheet<SkillBuffSheet>(world),
                GetSheet<StatBuffSheet>(world),
                GetSheet<SkillActionBuffSheet>(world),
                GetSheet<ActionBuffSheet>(world),
                GetSheet<CharacterSheet>(world),
                GetSheet<CharacterLevelSheet>(world),
                GetSheet<EquipmentItemSetEffectSheet>(world),
                GetSheet<WeeklyArenaRewardSheet>(world)
            );
        }

        public static RankingSimulatorSheets GetRankingSimulatorSheets(IWorld world)
        {
            return new RankingSimulatorSheets(
                GetSheet<MaterialItemSheet>(world),
                GetSheet<SkillSheet>(world),
                GetSheet<SkillBuffSheet>(world),
                GetSheet<StatBuffSheet>(world),
                GetSheet<SkillActionBuffSheet>(world),
                GetSheet<ActionBuffSheet>(world),
                GetSheet<CharacterSheet>(world),
                GetSheet<CharacterLevelSheet>(world),
                GetSheet<EquipmentItemSetEffectSheet>(world),
                GetSheet<WeeklyArenaRewardSheet>(world),
                GetSheet<RuneOptionSheet>(world)
            );
        }

        public static QuestSheet GetQuestSheet(IWorld world)
        {
            var questSheet = new QuestSheet();
            questSheet.Set(GetSheet<WorldQuestSheet>(world), false);
            questSheet.Set(GetSheet<CollectQuestSheet>(world), false);
            questSheet.Set(GetSheet<CombinationQuestSheet>(world), false);
            questSheet.Set(GetSheet<TradeQuestSheet>(world), false);
            questSheet.Set(GetSheet<MonsterQuestSheet>(world), false);
            questSheet.Set(GetSheet<ItemEnhancementQuestSheet>(world), false);
            questSheet.Set(GetSheet<GeneralQuestSheet>(world), false);
            questSheet.Set(GetSheet<ItemGradeQuestSheet>(world), false);
            questSheet.Set(GetSheet<ItemTypeCollectQuestSheet>(world), false);
            questSheet.Set(GetSheet<GoldQuestSheet>(world), false);
            questSheet.Set(GetSheet<CombinationEquipmentQuestSheet>(world));
            return questSheet;
        }

        public static AvatarSheets GetAvatarSheets(IWorld world)
        {
            return new AvatarSheets(
                GetSheet<WorldSheet>(world),
                GetQuestSheet(world),
                GetSheet<QuestRewardSheet>(world),
                GetSheet<QuestItemRewardSheet>(world),
                GetSheet<EquipmentItemRecipeSheet>(world),
                GetSheet<EquipmentItemSubRecipeSheet>(world)
            );
        }

        public static RankingState GetRankingState(IWorld world)
        {
            var value = GetState(world, Addresses.Ranking);
            if (value is null)
            {
                throw new FailedLoadStateException(nameof(RankingState0));
            }

            return new RankingState((Dictionary)value);
        }

        public static RankingState1 GetRankingState1(IWorld world)
        {
            var value = GetState(world, Addresses.Ranking);
            if (value is null)
            {
                throw new FailedLoadStateException(nameof(RankingState1));
            }

            return new RankingState1((Dictionary)value);
        }

        public static RankingState0 GetRankingState0(IWorld world)
        {
            var value = GetState(world, Addresses.Ranking);
            if (value is null)
            {
                throw new FailedLoadStateException(nameof(RankingState0));
            }

            return new RankingState0((Dictionary)value);
        }

        public static ShopState GetShopState(IWorld world)
        {
            var value = GetState(world, Addresses.Shop);
            if (value is null)
            {
                throw new FailedLoadStateException(nameof(ShopState));
            }

            return new ShopState((Dictionary)value);
        }

        public static (Address arenaInfoAddress, ArenaInfo arenaInfo, bool isNewArenaInfo) GetArenaInfo(
            IWorld world,
            Address weeklyArenaAddress,
            AvatarState avatarState,
            CharacterSheet characterSheet,
            CostumeStatSheet costumeStatSheet)
        {
            var arenaInfoAddress = weeklyArenaAddress.Derive(avatarState.address.ToByteArray());
            var isNew = false;
            ArenaInfo arenaInfo;
            if (TryGetState(world, arenaInfoAddress, out Dictionary rawArenaInfo))
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

        public static bool TryGetStakeState(
            IWorld world,
            Address agentAddress,
            out StakeState stakeState)
        {
            if (TryGetState(world, StakeState.DeriveAddress(agentAddress), out Dictionary dictionary))
            {
                stakeState = new StakeState(dictionary);
                return true;
            }

            stakeState = null;
            return false;
        }

        public static ArenaParticipants GetArenaParticipants(
            IWorld world,
            Address arenaParticipantsAddress,
            int id,
            int round)
        {
            return TryGetState(world, arenaParticipantsAddress, out List list)
                ? new ArenaParticipants(list)
                : new ArenaParticipants(id, round);
        }

        public static ArenaAvatarState GetArenaAvatarState(
            IWorld world,
            Address arenaAvatarStateAddress,
            AvatarState avatarState)
        {
            return TryGetState(world, arenaAvatarStateAddress, out List list)
                ? new ArenaAvatarState(list)
                : new ArenaAvatarState(avatarState);
        }

        public static bool TryGetArenaParticipants(
            IWorld world,
            Address arenaParticipantsAddress,
            out ArenaParticipants arenaParticipants)
        {
            if (TryGetState(world, arenaParticipantsAddress, out List list))
            {
                arenaParticipants = new ArenaParticipants(list);
                return true;
            }

            arenaParticipants = null;
            return false;
        }

        public static bool TryGetArenaAvatarState(
            IWorld world,
            Address arenaAvatarStateAddress,
            out ArenaAvatarState arenaAvatarState)
        {
            if (TryGetState(world, arenaAvatarStateAddress, out List list))
            {
                arenaAvatarState = new ArenaAvatarState(list);
                return true;
            }

            arenaAvatarState = null;
            return false;
        }

        public static bool TryGetArenaScore(
            IWorld world,
            Address arenaScoreAddress,
            out ArenaScore arenaScore)
        {
            if (TryGetState(world, arenaScoreAddress, out List list))
            {
                arenaScore = new ArenaScore(list);
                return true;
            }

            arenaScore = null;
            return false;
        }

        public static bool TryGetArenaInformation(
            IWorld world,
            Address arenaInformationAddress,
            out ArenaInformation arenaInformation)
        {
            if (TryGetState(world, arenaInformationAddress, out List list))
            {
                arenaInformation = new ArenaInformation(list);
                return true;
            }

            arenaInformation = null;
            return false;
        }

        public static CrystalCostState GetCrystalCostState(
            IWorld world,
            Address address)
        {
            return TryGetState(world, address, out List rawState)
                ? new CrystalCostState(address, rawState)
                : new CrystalCostState(address, 0 * CrystalCalculator.CRYSTAL);
        }

        public static (
            CrystalCostState DailyCostState,
            CrystalCostState WeeklyCostState,
            CrystalCostState PrevWeeklyCostState,
            CrystalCostState BeforePrevWeeklyCostState
            ) GetCrystalCostStates(IWorld world, long blockIndex, long interval)
        {
            int dailyCostIndex = (int) (blockIndex / CrystalCostState.DailyIntervalIndex);
            int weeklyCostIndex = (int) (blockIndex / interval);
            Address dailyCostAddress = Addresses.GetDailyCrystalCostAddress(dailyCostIndex);
            CrystalCostState dailyCostState = GetCrystalCostState(world, dailyCostAddress);
            Address weeklyCostAddress = Addresses.GetWeeklyCrystalCostAddress(weeklyCostIndex);
            CrystalCostState weeklyCostState = GetCrystalCostState(world, weeklyCostAddress);
            CrystalCostState prevWeeklyCostState = null;
            CrystalCostState beforePrevWeeklyCostState = null;
            if (weeklyCostIndex > 1)
            {
                Address prevWeeklyCostAddress =
                    Addresses.GetWeeklyCrystalCostAddress(weeklyCostIndex - 1);
                prevWeeklyCostState = GetCrystalCostState(world, prevWeeklyCostAddress);
                Address beforePrevWeeklyCostAddress =
                    Addresses.GetWeeklyCrystalCostAddress(weeklyCostIndex - 2);
                beforePrevWeeklyCostState = GetCrystalCostState(world, beforePrevWeeklyCostAddress);
            }

            return (dailyCostState, weeklyCostState, prevWeeklyCostState,
                beforePrevWeeklyCostState);
        }

        public static void ValidateWorldId(IWorld world, Address avatarAddress, int worldId)
        {
            if (worldId > 1)
            {
                if (worldId == GameConfig.MimisbrunnrWorldId)
                {
                    throw new InvalidWorldException();
                }

                var unlockedWorldIdsAddress = avatarAddress.Derive("world_ids");

                // Unlock First.
                if (!TryGetState(world, unlockedWorldIdsAddress, out List rawIds))
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
            IWorld world,
            Address avatarAddress,
            int raidId)
        {
            return GetRaiderState(world, Addresses.GetRaiderAddress(avatarAddress, raidId));
        }

        public static RaiderState GetRaiderState(
            IWorld world,
            Address raiderAddress)
        {
            if (TryGetState(world, raiderAddress, out List rawRaider))
            {
                return new RaiderState(rawRaider);
            }

            throw new FailedLoadStateException("can't find RaiderState.");
        }

        public static Dictionary<Type, (Address address, ISheet sheet)> GetSheetsV100291(
            IWorld world,
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

            return GetSheets(world, sheetTypeList.Distinct().ToArray());
        }

        public static Dictionary<Type, (Address address, ISheet sheet)> GetSheetsV1(
            IWorld world,
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

            return GetSheets(world, sheetTypeList.Distinct().ToArray());
        }

        public static IValue GetInventoryState(
            IWorld world,
            Address inventoryAddr)
        {
            var inventoryState = GetState(world, inventoryAddr);
            if (inventoryState is null || inventoryState is Null)
            {
                throw new StateNullException(inventoryAddr);
            }

            return inventoryState;
        }

        public static Inventory GetInventory(
            IWorld world,
            Address inventoryAddr)
        {
            var inventoryState = GetInventoryState(world, inventoryAddr);
            return new Inventory((List)inventoryState);
        }
    }
}
