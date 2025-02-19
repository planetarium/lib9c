using System;
using System.Collections.Generic;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.State;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Module;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// An action to claim remained monster collection rewards and to migrate
    /// <see cref="MonsterCollectionState"/> into <see cref="LegacyStakeState"/> without cancellation, to
    /// keep its staked period.
    /// </summary>
    [ActionType("migrate_monster_collection")]
    public class MigrateMonsterCollection : ActionBase, IMigrateMonsterCollectionV1
    {
        public Address AvatarAddress { get; private set; }

        Address IMigrateMonsterCollectionV1.AvatarAddress => AvatarAddress;

        public MigrateMonsterCollection(Address avatarAddress)
        {
            AvatarAddress = avatarAddress;
        }

        public MigrateMonsterCollection()
        {
        }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", "migrate_monster_collection")
            .Add("values", Dictionary.Empty.Add(AvatarAddressKey, AvatarAddress.Serialize()));

        public override void LoadPlainValue(IValue plainValue)
        {
            var dictionary = (Dictionary)((Dictionary)plainValue)["values"];
            AvatarAddress = dictionary[AvatarAddressKey].ToAddress();
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}MigrateMonsterCollection exec started", addressesHex);
            if (states.TryGetLegacyStakeState(context.Signer, out LegacyStakeState _))
            {
                throw new InvalidOperationException("The user has already staked.");
            }

            try
            {
                states = ClaimMonsterCollectionReward(context, AvatarAddress, addressesHex);
            }
            catch (Exception e)
            {
                Log.Error(
                    e,
                    "An exception({Exception}) occurred while claiming monster collection rewards.",
                    e);
            }

            var agentState = states.GetAgentState(context.Signer);
            var currency = states.GetGoldCurrency();

            Address collectionAddress = MonsterCollectionState.DeriveAddress(context.Signer, agentState.MonsterCollectionRound);
            if (!states.TryGetLegacyState(collectionAddress, out Dictionary stateDict))
            {
                throw new FailedLoadStateException($"Aborted as the monster collection state failed to load.");
            }

            var monsterCollectionState = new MonsterCollectionState(stateDict);
            var migratedStakeStateAddress = LegacyStakeState.DeriveAddress(context.Signer);
            var migratedStakeState = new LegacyStakeState(
                migratedStakeStateAddress,
                monsterCollectionState.StartedBlockIndex,
                monsterCollectionState.ReceivedBlockIndex,
                monsterCollectionState.ExpiredBlockIndex,
                new LegacyStakeState.StakeAchievements());

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}MigrateMonsterCollection Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states.RemoveLegacyState(monsterCollectionState.address)
                .SetLegacyState(migratedStakeStateAddress, migratedStakeState.SerializeV2())
                .TransferAsset(
                    context,
                    monsterCollectionState.address,
                    migratedStakeStateAddress,
                    states.GetBalance(monsterCollectionState.address, currency));
        }

        private static IWorld ClaimMonsterCollectionReward(IActionContext context, Address avatarAddress, string addressesHex)
        {
            const long MonsterCollectionRewardEndBlockIndex = 4_481_909;

            IWorld states = context.PreviousState;
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}ClaimMonsterCollection exec started", addressesHex);

            var agentState = states.GetAgentState(context.Signer);
            if (!states.TryGetAvatarState(context.Signer, avatarAddress, out AvatarState avatarState))
            {
                throw new FailedLoadStateException($"Aborted as the avatar state of the signer failed to load.");
            }

            Address collectionAddress = MonsterCollectionState.DeriveAddress(context.Signer, agentState.MonsterCollectionRound);

            if (!states.TryGetLegacyState(collectionAddress, out Dictionary stateDict))
            {
                throw new FailedLoadStateException($"Aborted as the monster collection state failed to load.");
            }

            var monsterCollectionState = new MonsterCollectionState(stateDict);
            List<MonsterCollectionRewardSheet.RewardInfo> rewards =
                monsterCollectionState.CalculateRewards(
                    states.GetSheet<MonsterCollectionRewardSheet>(),
                    Math.Min(MonsterCollectionRewardEndBlockIndex, context.BlockIndex)
                );

            if (rewards.Count == 0)
            {
                throw new RequiredBlockIndexException($"{collectionAddress} is not available yet");
            }

            var random = context.GetRandom();
            Guid id = random.GenerateRandomGuid();
            var result = new MonsterCollectionResult(id, avatarAddress, rewards);
            var mail = new MonsterCollectionMail(result, context.BlockIndex, id, context.BlockIndex);
            avatarState.Update(mail);

            ItemSheet itemSheet = states.GetItemSheet();
            foreach (MonsterCollectionRewardSheet.RewardInfo rewardInfo in rewards)
            {
                ItemSheet.Row row = itemSheet[rewardInfo.ItemId];
                ItemBase item = row is MaterialItemSheet.Row materialRow
                    ? ItemFactory.CreateTradableMaterial(materialRow)
                    : ItemFactory.CreateItem(row, random);
                avatarState.inventory.AddItem(item, rewardInfo.Quantity);
            }
            monsterCollectionState.Claim(context.BlockIndex);

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}ClaimMonsterCollection Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetAvatarState(avatarAddress, avatarState)
                .SetLegacyState(collectionAddress, monsterCollectionState.Serialize());
        }
    }
}
