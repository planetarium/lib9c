using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("claim_monster_collection_reward2")]
    public class ClaimMonsterCollectionReward : GameAction
    {
        public Address avatarAddress;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            Address collectionAddress = MonsterCollectionState.DeriveAddress(context.Signer, 0);
            Address inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            if (context.Rehearsal)
            {
                return states
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(collectionAddress, MarkChanged);
            }

            if (!states.TryGetAvatarStateV2(context.Signer, avatarAddress, out AvatarState avatarState))
            {
                throw new FailedLoadStateException($"Aborted as the avatar state of the signer failed to load.");
            }

            if (!states.TryGetState(collectionAddress, out Dictionary stateDict))
            {
                throw new FailedLoadStateException($"Aborted as the monster collection state failed to load.");
            }

            var monsterCollectionState = new MonsterCollectionState(stateDict);

            int step = (int)Math.DivRem(
                context.BlockIndex - monsterCollectionState.StartedBlockIndex,
                MonsterCollectionState.RewardInterval,
                out _
            );
            if (monsterCollectionState.ReceivedBlockIndex > 0)
            {
                int previousStep = (int)Math.DivRem(
                    monsterCollectionState.ReceivedBlockIndex - monsterCollectionState.StartedBlockIndex,
                    MonsterCollectionState.RewardInterval,
                    out _
                );
                step -= previousStep;
            }

            if (step < 1)
            {
                throw new RequiredBlockIndexException($"{collectionAddress} is not available yet");
            }

            MonsterCollectionRewardSheet monsterCollectionRewardSheet = 
                states.GetSheet<MonsterCollectionRewardSheet>();
            List<MonsterCollectionRewardSheet.RewardInfo> rewards =
                monsterCollectionRewardSheet[monsterCollectionState.Level].Rewards
                .GroupBy(ri => ri.ItemId)
                .Select(g => new MonsterCollectionRewardSheet.RewardInfo(
                        g.Key,
                        g.Sum(ri => ri.Quantity) * step))
                .ToList();
            Guid id = context.Random.GenerateRandomGuid();
            var result = new MonsterCollectionResult(id, avatarAddress, rewards);
            var mail = new MonsterCollectionMail(result, context.BlockIndex, id, context.BlockIndex);
            avatarState.UpdateV3(mail);

            ItemSheet itemSheet = states.GetItemSheet();
            foreach (MonsterCollectionRewardSheet.RewardInfo rewardInfo in rewards)
            {
                ItemSheet.Row row = itemSheet[rewardInfo.ItemId];
                ItemBase item = row is MaterialItemSheet.Row materialRow
                    ? ItemFactory.CreateTradableMaterial(materialRow)
                    : ItemFactory.CreateItem(row, context.Random);
                avatarState.inventory.AddItem(item, rewardInfo.Quantity);
            }
            monsterCollectionState.Receive(context.BlockIndex);

            return states
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(collectionAddress, monsterCollectionState.SerializeV2());
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
