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
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/602
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionType("claim_monster_collection_reward3")]
    [ActionObsolete(ActionObsoleteConfig.V200020AccidentObsoleteIndex)]
    public class ClaimMonsterCollectionReward : GameAction, IClaimMonsterCollectionRewardV2
    {
        public const long MonsterCollectionRewardEndBlockIndex = 4_481_909;
        public Address avatarAddress;

        Address IClaimMonsterCollectionRewardV2.AvatarAddress => avatarAddress;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            return Claim(context, avatarAddress, addressesHex);
        }

        public static IAccountStateDelta Claim(IActionContext context, Address avatarAddress, string addressesHex)
        {
            IAccountStateDelta states = context.PreviousState;
            Address inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            Address worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            Address questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}ClaimMonsterCollection exec started", addressesHex);

            if (context.Rehearsal)
            {
                return states
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 0), MarkChanged)
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 1), MarkChanged)
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 2), MarkChanged)
                    .SetState(MonsterCollectionState.DeriveAddress(context.Signer, 3), MarkChanged);
            }

            if (!states.TryGetAgentAvatarStatesV2(context.Signer, avatarAddress, out AgentState agentState, out AvatarState avatarState, out _))
            {
                throw new FailedLoadStateException($"Aborted as the avatar state of the signer failed to load.");
            }

            Address collectionAddress = MonsterCollectionState.DeriveAddress(context.Signer, agentState.MonsterCollectionRound);

            if (!states.TryGetState(collectionAddress, out Dictionary stateDict))
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

            Guid id = context.Random.GenerateRandomGuid();
            var result = new MonsterCollectionResult(id, avatarAddress, rewards);
            var mail = new MonsterCollectionMail(result, context.BlockIndex, id, context.BlockIndex);
            avatarState.Update(mail);

            ItemSheet itemSheet = states.GetItemSheet();
            foreach (MonsterCollectionRewardSheet.RewardInfo rewardInfo in rewards)
            {
                ItemSheet.Row row = itemSheet[rewardInfo.ItemId];
                ItemBase item = row is MaterialItemSheet.Row materialRow
                    ? ItemFactory.CreateTradableMaterial(materialRow)
                    : ItemFactory.CreateItem(row, context.Random);
                avatarState.inventory.AddItem(item, rewardInfo.Quantity);
            }
            monsterCollectionState.Claim(context.BlockIndex);

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}ClaimMonsterCollection Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(collectionAddress, monsterCollectionState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            [AvatarAddressKey] = avatarAddress.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue[AvatarAddressKey].ToAddress();
        }
    }
}
