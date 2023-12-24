using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Model.State;
using Nekoyume.Module;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/618
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionType("migration_avatar_state")]
    public class MigrationAvatarState : GameAction, IMigrationAvatarStateV1
    {
        public List<Dictionary> avatarStates;

        IEnumerable<IValue> IMigrationAvatarStateV1.AvatarStates => avatarStates;

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;

            CheckPermission(context);

            foreach (var rawAvatar in avatarStates)
            {
                var v1 = new AvatarState(rawAvatar);
                var inventoryAddress = v1.address.Derive(LegacyInventoryKey);
                var worldInformationAddress = v1.address.Derive(LegacyWorldInformationKey);
                var questListAddress = v1.address.Derive(LegacyQuestListKey);
                if (states.GetState(inventoryAddress) is null)
                {
                    states = states.SetState(inventoryAddress, v1.inventory.Serialize());
                }
                if (states.GetState(worldInformationAddress) is null)
                {
                    states = states.SetState(worldInformationAddress, v1.worldInformation.Serialize());
                }
                if (states.GetState(questListAddress) is null)
                {
                    states = states.SetState(questListAddress, v1.questList.Serialize());
                }

                var v2 = states.GetAvatarState(v1.address);
                if (v2.inventory is null || v2.worldInformation is null || v2.questList is null)
                {
                    throw new FailedLoadStateException(v1.address.ToHex());
                }
            }

            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["a"] = avatarStates.Serialize(),
        }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarStates = plainValue["a"].ToList(i => (Dictionary)i);
        }

        public static IValue LegacySerializeV1(AvatarState avatarState) =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)LegacyNameKey] = (Text)avatarState.name,
                [(Text)LegacyCharacterIdKey] = (Integer)avatarState.characterId,
                [(Text)LegacyLevelKey] = (Integer)avatarState.level,
                [(Text)ExpKey] = (Integer)avatarState.exp,
                [(Text)LegacyInventoryKey] = avatarState.inventory.Serialize(),
                [(Text)LegacyWorldInformationKey] = avatarState.worldInformation.Serialize(),
                [(Text)LegacyUpdatedAtKey] = avatarState.updatedAt.Serialize(),
                [(Text)LegacyAgentAddressKey] = avatarState.agentAddress.Serialize(),
                [(Text)LegacyQuestListKey] = avatarState.questList.Serialize(),
                [(Text)LegacyMailBoxKey] = avatarState.mailBox.Serialize(),
                [(Text)LegacyBlockIndexKey] = (Integer)avatarState.blockIndex,
                [(Text)LegacyDailyRewardReceivedIndexKey] = (Integer)avatarState.dailyRewardReceivedIndex,
                [(Text)LegacyActionPointKey] = (Integer)avatarState.actionPoint,
                [(Text)LegacyStageMapKey] = avatarState.stageMap.Serialize(),
                [(Text)LegacyMonsterMapKey] = avatarState.monsterMap.Serialize(),
                [(Text)LegacyItemMapKey] = avatarState.itemMap.Serialize(),
                [(Text)LegacyEventMapKey] = avatarState.eventMap.Serialize(),
                [(Text)LegacyHairKey] = (Integer)avatarState.hair,
                [(Text)LensKey] = (Integer)avatarState.lens,
                [(Text)LegacyEarKey] = (Integer)avatarState.ear,
                [(Text)LegacyTailKey] = (Integer)avatarState.tail,
                [(Text)LegacyCombinationSlotAddressesKey] = avatarState.combinationSlotAddresses
                    .OrderBy(i => i)
                    .Select(i => i.Serialize())
                    .Serialize(),
                [(Text)LegacyNonceKey] = avatarState.Nonce.Serialize(),
                [(Text)LegacyRankingMapAddressKey] = avatarState.RankingMapAddress.Serialize(),
                [(Text)AddressKey] = avatarState.address.Serialize(),
            });
#pragma warning restore LAA1002

        public static IValue LegacySerializeV2(AvatarState avatarState) =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)NameKey] = (Text)avatarState.name,
                [(Text)CharacterIdKey] = (Integer)avatarState.characterId,
                [(Text)LevelKey] = (Integer)avatarState.level,
                [(Text)ExpKey] = (Integer)avatarState.exp,
                [(Text)UpdatedAtKey] = avatarState.updatedAt.Serialize(),
                [(Text)AgentAddressKey] = avatarState.agentAddress.Serialize(),
                [(Text)MailBoxKey] = avatarState.mailBox.Serialize(),
                [(Text)BlockIndexKey] = (Integer)avatarState.blockIndex,
                [(Text)DailyRewardReceivedIndexKey] = (Integer)avatarState.dailyRewardReceivedIndex,
                [(Text)ActionPointKey] = (Integer)avatarState.actionPoint,
                [(Text)StageMapKey] = avatarState.stageMap.Serialize(),
                [(Text)MonsterMapKey] = avatarState.monsterMap.Serialize(),
                [(Text)ItemMapKey] = avatarState.itemMap.Serialize(),
                [(Text)EventMapKey] = avatarState.eventMap.Serialize(),
                [(Text)HairKey] = (Integer)avatarState.hair,
                [(Text)LensKey] = (Integer)avatarState.lens,
                [(Text)EarKey] = (Integer)avatarState.ear,
                [(Text)TailKey] = (Integer)avatarState.tail,
                [(Text)CombinationSlotAddressesKey] = avatarState.combinationSlotAddresses
                    .OrderBy(i => i)
                    .Select(i => i.Serialize())
                    .Serialize(),
                [(Text)RankingMapAddressKey] = avatarState.RankingMapAddress.Serialize(),
                [(Text)AddressKey] = avatarState.address.Serialize(),
            });
#pragma warning restore LAA1002
    }
}
