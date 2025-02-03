using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using Libplanet.Common;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume
{
    public static class Addresses
    {
        public static readonly Address Shop                  = new("0000000000000000000000000000000000000000");
        public static readonly Address Ranking               = new("0000000000000000000000000000000000000001");
        public static readonly Address WeeklyArena           = new("0000000000000000000000000000000000000002");
        public static readonly Address TableSheet            = new("0000000000000000000000000000000000000003");
        public static readonly Address GameConfig            = new("0000000000000000000000000000000000000004");
        public static readonly Address RedeemCode            = new("0000000000000000000000000000000000000005");
        public static readonly Address Admin                 = new("0000000000000000000000000000000000000006");
        public static readonly Address PendingActivation     = new("0000000000000000000000000000000000000007");
        public static readonly Address ActivatedAccount      = new("0000000000000000000000000000000000000008");
        public static readonly Address Blacksmith            = new("0000000000000000000000000000000000000009");
        public static readonly Address GoldCurrency          = new("000000000000000000000000000000000000000a");
        public static readonly Address GoldDistribution      = new("000000000000000000000000000000000000000b");
        public static readonly Address AuthorizedMiners      = new("000000000000000000000000000000000000000c");
        public static readonly Address Credits               = new("000000000000000000000000000000000000000d");
        public static readonly Address UnlockWorld           = new("000000000000000000000000000000000000000e");
        public static readonly Address UnlockEquipmentRecipe = new("000000000000000000000000000000000000000f");
        public static readonly Address MaterialCost          = new("0000000000000000000000000000000000000010");
        public static readonly Address StageRandomBuff       = new("0000000000000000000000000000000000000011");
        public static readonly Address Arena                 = new("0000000000000000000000000000000000000012");
        public static readonly Address SuperCraft            = new("0000000000000000000000000000000000000013");
        public static readonly Address EventDungeon          = new("0000000000000000000000000000000000000014");
        public static readonly Address Raid                  = new("0000000000000000000000000000000000000015");
        public static readonly Address Rune                  = new("0000000000000000000000000000000000000016");
        public static readonly Address Market                = new("0000000000000000000000000000000000000017");
        public static readonly Address GarageWallet          = new("0000000000000000000000000000000000000018");
        public static readonly Address AssetMinters          = new("0000000000000000000000000000000000000019");
        public static readonly Address Agent                 = new("000000000000000000000000000000000000001a");
        public static readonly Address Avatar                = new("000000000000000000000000000000000000001b");
        public static readonly Address Inventory             = new("000000000000000000000000000000000000001c");
        public static readonly Address WorldInformation      = new("000000000000000000000000000000000000001d");
        public static readonly Address QuestList             = new("000000000000000000000000000000000000001e");
        public static readonly Address Collection            = new("000000000000000000000000000000000000001f");
        public static readonly Address DailyReward           = new("0000000000000000000000000000000000000020");
        public static readonly Address ActionPoint           = new("0000000000000000000000000000000000000021");
        public static readonly Address RuneState             = new("0000000000000000000000000000000000000022");

        // Custom Equipment Craft
        public static readonly Address Relationship          = new("0000000000000000000000000000000000000023");

        public static readonly Address CombinationSlot       = new("0000000000000000000000000000000000000024");
        public static readonly Address ClaimedGiftIds        = new("0000000000000000000000000000000000000025");
        public static readonly Address PatrolReward          = new("0000000000000000000000000000000000000026");

        // Arena
        public static readonly Address Battle                = new("0000000000000000000000000000000000000027");

        // Adventure Boss
        public static readonly Address AdventureBoss         = new("0000000000000000000000000000000000000100");
        public static readonly Address BountyBoard           = new("0000000000000000000000000000000000000101");
        public static readonly Address ExploreBoard          = new("0000000000000000000000000000000000000102");
        public static readonly Address ExplorerList          = new("0000000000000000000000000000000000000103");

        public static readonly Address MortgagePool = new Address("0000000000000000000000000000000000100000");
        public static readonly Address GasPool = new Address("0000000000000000000000000000000000100001");

        /// <summary>
        /// An address of an account having <see cref="Model.Swap.SwapPool"/>.
        /// </summary>
        public static readonly Address SwapPool = new Address("0000000000000000000000000000000000100002");

        #region Guild

        /// <summary>
        /// An address of an account having <see cref="Nekoyume.Model.Guild.Guild"/>.
        /// </summary>
        public static readonly Address Guild = new("0000000000000000000000000000000000000200");

        /// <summary>
        /// An address of an account having <see cref="Bencodex.Types.Integer"/> which means the number of the guild.
        /// </summary>
        public static readonly Address GuildMemberCounter = new("0000000000000000000000000000000000000201");

        /// <summary>
        /// An address of an account having <see cref="Nekoyume.Model.Guild.GuildApplication"/>.
        /// </summary>
        public static readonly Address GuildApplication = new("0000000000000000000000000000000000000202");

        /// <summary>
        /// An address of an account having <see cref="Nekoyume.Model.Guild.GuildParticipant"/>
        /// </summary>
        public static readonly Address GuildParticipant = new("0000000000000000000000000000000000000203");

        /// <summary>
        /// An address of an account having <see cref="Nekoyume.Model.Guild.GuildRejoinCooldown"/>
        /// </summary>
        public static readonly Address GuildRejoinCooldown =
            new Address("0000000000000000000000000000000000000204");

        /// <summary>
        /// Build an <see cref="Address"/> of an <see cref="Libplanet.Action.State.Account"/>,
        /// represented as `agentAddress` ↔ <see cref="Bencodex.Types.Boolean"/>, indicates whether
        /// the `agentAddress` is banned.
        /// </summary>
        /// <param name="guildAddress">The guild address.</param>
        /// <returns>An account address.</returns>
        public static Address GetGuildBanAccountAddress(Address guildAddress) =>
            guildAddress.Derive("guild.banned");

        public static readonly Address EmptyAccountAddress = new("ffffffffffffffffffffffffffffffffffffffff");

        /// <summary>
        /// An address of an account having <see cref="Delegation.DelegateeMetadata"/>.
        /// </summary>
        public static readonly Address GuildDelegateeMetadata
            = new Address("0000000000000000000000000000000000000210");

        /// <summary>
        /// An address of an account having <see cref="Delegation.DelegatorMetadata"/>.
        /// </summary>
        public static readonly Address GuildDelegatorMetadata
            = new Address("0000000000000000000000000000000000000211");

        /// <summary>
        /// An address of an account having <see cref="Delegation.Bond"/>.
        /// </summary>
        public static readonly Address GuildBond
            = new Address("0000000000000000000000000000000000000212");

        /// <summary>
        /// An address of an account having <see cref="Delegation.UnbondLockIn"/>.
        /// </summary>
        public static readonly Address GuildUnbondLockIn
            = new Address("0000000000000000000000000000000000000213");

        /// <summary>
        /// An address of an account having <see cref="Delegation.RebondGrace"/>.
        /// </summary>
        public static readonly Address GuildRebondGrace
            = new Address("0000000000000000000000000000000000000214");

        /// <summary>
        /// An address of an account having <see cref="Delegation.LumpSumRewardsRecord"/>.
        /// </summary>
        public static readonly Address GuildLumpSumRewardsRecord
            = new Address("0000000000000000000000000000000000000215");

        /// <summary>
        /// An address of an account having <see cref="Delegation.UnbondingSet"/>.
        /// </summary>
        public static readonly Address GuildUnbondingSet
            = new Address("0000000000000000000000000000000000000216");

        /// <summary>
        /// An address of an account having <see cref="Delegation.RewardBase"/>.
        /// </summary>
        public static readonly Address GuildRewardBase
            = new Address("0000000000000000000000000000000000000217");

        #endregion

        #region Validator
        /// <summary>
        /// An address of an account having <see cref="ValidatorDelegation.ValidatorDelegatee"/>.
        /// </summary>
        public static readonly Address ValidatorDelegatee
            = new Address("0000000000000000000000000000000000000300");

        /// <summary>
        /// An address of an account having <see cref="ValidatorDelegation.ValidatorDelegator"/>.
        /// </summary>
        public static readonly Address ValidatorDelegator
            = new Address("0000000000000000000000000000000000000301");

        /// <summary>
        /// An address of an account having <see cref="Delegation.DelegateeMetadata"/>.
        /// </summary>
        public static readonly Address ValidatorDelegateeMetadata
            = new Address("0000000000000000000000000000000000000302");

        /// <summary>
        /// An address of an account having <see cref="Delegation.DelegatorMetadata"/>.
        /// </summary>
        public static readonly Address ValidatorDelegatorMetadata
            = new Address("0000000000000000000000000000000000000303");

        /// <summary>
        /// An address of an account having <see cref="Delegation.Bond"/>.
        /// </summary>
        public static readonly Address ValidatorBond
            = new Address("0000000000000000000000000000000000000304");

        /// <summary>
        /// An address of an account having <see cref="Delegation.UnbondLockIn"/>.
        /// </summary>
        public static readonly Address ValidatorUnbondLockIn
            = new Address("0000000000000000000000000000000000000305");

        /// <summary>
        /// An address of an account having <see cref="Delegation.RebondGrace"/>.
        /// </summary>
        public static readonly Address ValidatorRebondGrace
            = new Address("0000000000000000000000000000000000000306");

        /// <summary>
        /// An address of an account having <see cref="Delegation.LumpSumRewardsRecord"/>.
        /// </summary>
        public static readonly Address ValidatorLumpSumRewardsRecord
            = new Address("0000000000000000000000000000000000000307");

        /// <summary>
        /// An address of an account having <see cref="Delegation.UnbondingSet"/>.
        /// </summary>
        public static readonly Address ValidatorUnbondingSet
            = new Address("0000000000000000000000000000000000000308");

        /// <summary>
        /// An address of an account having <see cref="ValidatorDelegation.AbstainHistory"/>.
        /// </summary>
        public static readonly Address AbstainHistory
            = new Address("0000000000000000000000000000000000000309");

        /// <summary>
        /// An address of an account having <see cref="ValidatorDelegation.ValidatorList"/>.
        /// </summary>
        public static readonly Address ValidatorList
            = new Address("0000000000000000000000000000000000000310");

        /// <summary>
        /// An address for reward pool of validators.
        /// </summary>
        public static readonly Address RewardPool
            = new Address("0000000000000000000000000000000000000311");

        /// <summary>
        /// An address for community fund.
        /// </summary>
        public static readonly Address CommunityPool
            = new Address("0000000000000000000000000000000000000312");

        /// <summary>
        /// An address for non validator.
        /// </summary>
        [Obsolete("It is not used anymore.")]
        public static readonly Address NonValidatorDelegatee
            = new Address("0000000000000000000000000000000000000313");

        /// <summary>
        /// An address of an account having <see cref="Delegation.RewardBase"/>.
        /// </summary>
        public static readonly Address ValidatorRewardBase
            = new Address("0000000000000000000000000000000000000314");

        #endregion

        #region Migration

        /// <summary>
        /// An account address for migration.
        /// </summary>
        public static readonly Address Migration
            = new Address("0000000000000000000000000000000000000400");

        #endregion

        public static Address GetSheetAddress<T>() where T : ISheet => GetSheetAddress(typeof(T).Name);

        public static Address GetSheetAddress(string sheetName) => TableSheet.Derive(sheetName);

        public static Address GetItemAddress(Guid itemId) => Blacksmith.Derive(itemId.ToString());

        public static Address GetDailyCrystalCostAddress(int index)
        {
            return MaterialCost.Derive($"daily_{index.ToString(CultureInfo.InvariantCulture)}");
        }

        public static Address GetWeeklyCrystalCostAddress(int index)
        {
            return MaterialCost.Derive($"weekly_{index.ToString(CultureInfo.InvariantCulture)}");
        }

        public static Address GetSkillStateAddressFromAvatarAddress(Address avatarAddress) =>
            avatarAddress.Derive("has_buff");

        public static Address GetHammerPointStateAddress(Address avatarAddress, int recipeId) =>
            avatarAddress.Derive($"hammer_{recipeId}");

        public static Address GetWorldBossAddress(int raidId) => Raid.Derive($"{raidId}");
        public static Address GetWorldBossKillRewardRecordAddress(Address avatarAddress, int raidId) => avatarAddress.Derive($"reward_info_{raidId}");
        public static Address GetRaiderAddress(Address avatarAddress, int raidId) => avatarAddress.Derive($"{raidId}");

        public static Address GetRaiderListAddress(int raidId) =>
            Raid.Derive($"raider_list_{raidId}");

        public static Address GetAvatarAddress(Address agentAddr, int index)
        {
            if (index < 0 ||
                index >= Nekoyume.GameConfig.SlotCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"Index must be between 0 and {Nekoyume.GameConfig.SlotCount - 1}.");
            }

            var deriveKey = string.Format(
                CultureInfo.InvariantCulture,
                CreateAvatar.DeriveFormat,
                index);
            return agentAddr.Derive(deriveKey);
        }

        public static Address GetInventoryAddress(Address agentAddr, int avatarIndex)
        {
            return GetAvatarAddress(agentAddr, avatarIndex)
                .Derive(LegacyInventoryKey);
        }

        public static Address GetCombinationSlotAddress(Address avatarAddr, int index)
        {
            var deriveKey = string.Format(
                CultureInfo.InvariantCulture,
                CombinationSlotState.DeriveFormat,
                index);
            return avatarAddr.Derive(deriveKey);
        }

        public static Address GetGarageBalanceAddress(Address agentAddr)
        {
            return agentAddr.Derive("garage-balance");
        }

        public static Address GetGarageAddress(
            Address agentAddr,
            HashDigest<SHA256> fungibleId)
        {
            return agentAddr
                .Derive("garage")
                .Derive(fungibleId.ToString());
        }

        public static bool CheckAvatarAddrIsContainedInAgent(
            Address agentAddr,
            Address avatarAddr) =>
            Enumerable.Range(0, Nekoyume.GameConfig.SlotCount)
                .Select(index => GetAvatarAddress(agentAddr, index))
                .Contains(avatarAddr);

        public static bool CheckAgentHasPermissionOnBalanceAddr(
            Address agentAddr,
            Address balanceAddr) =>
            agentAddr == balanceAddr ||
            Enumerable.Range(0, Nekoyume.GameConfig.SlotCount)
                .Select(index => GetAvatarAddress(agentAddr, index))
                .Contains(balanceAddr);

        public static bool CheckInventoryAddrIsContainedInAgent(
            Address agentAddr,
            Address inventoryAddr) =>
            Enumerable.Range(0, Nekoyume.GameConfig.SlotCount)
                .Select(index => GetAvatarAddress(agentAddr, index))
                .Contains(inventoryAddr);

        public static Address AdventureSeasonAddress(long season) =>
            AdventureBoss.Derive(season.ToString(CultureInfo.InvariantCulture));

        /// <summary>
        /// Get the account address of an arena participant.
        /// </summary>
        /// <returns>
        /// The account address of an arena participant.
        /// "0100000000000000000000000000000000000000" ~ "0199999999999999999999999999999999999999".
        /// </returns>
        public static Address GetArenaParticipantAccountAddress(int championshipId, int round)
        {
            var hex = $"01{championshipId:D36}{round:D2}";
            return new Address(hex);
        }

        /// <summary>
        /// Check if the given address is within the range of arena participant account addresses.
        /// "0100000000000000000000000000000000000000" ~ "0199999999999999999999999999999999999999".
        /// </summary>
        public static bool IsArenaParticipantAccountAddress(Address address)
        {
            const string lowerBound = "0100000000000000000000000000000000000000";
            const string upperBound = "0199999999999999999999999999999999999999";
            var hex = address.ToHex();

            return string.CompareOrdinal(hex, lowerBound) >= 0 &&
                   string.CompareOrdinal(hex, upperBound) <= 0;
        }
    }
}
