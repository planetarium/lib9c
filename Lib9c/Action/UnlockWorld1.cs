using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("unlock_world")]
    public class UnlockWorld1: GameAction
    {
        public List<int> WorldIds;
        public Address AvatarAddress;
        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var worldInformationAddress = AvatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = AvatarAddress.Derive(LegacyQuestListKey);
            var inventoryAddress = AvatarAddress.Derive(LegacyInventoryKey);
            var unlockedWorldIdsAddress = AvatarAddress.Derive("world_ids");
            if (context.Rehearsal)
            {
                return states
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(AvatarAddress, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, context.Signer, Addresses.UnlockWorld);
            }

            if (!WorldIds.Any() || WorldIds.Any(i => i < 2 || i == GameConfig.MimisbrunnrWorldId))
            {
                throw new InvalidWorldException();
            }

            WorldInformation worldInformation;
            AvatarState avatarState = null;
            bool migrationRequired = false;

            if (states.TryGetState(worldInformationAddress, out Dictionary rawInfo))
            {
                worldInformation = new WorldInformation(rawInfo);
            }
            else
            {
                // AvatarState migration required.
                if (states.TryGetAvatarState(context.Signer, AvatarAddress, out avatarState))
                {
                    worldInformation = avatarState.worldInformation;
                    migrationRequired = true;
                }
                else
                {
                    // Invalid Address.
                    throw new FailedLoadStateException($"Can't find AvatarState {AvatarAddress}");
                }
            }

            List<int> unlockedIds = states.TryGetState(unlockedWorldIdsAddress, out List rawIds)
                ? rawIds.ToList(StateExtensions.ToInteger)
                : new List<int>
                {
                    1,
                    GameConfig.MimisbrunnrWorldId,
                };

            var sortedWorldIds = WorldIds.OrderBy(i => i).ToList();
            var worldUnlockSheet = states.GetSheet<WorldUnlockSheet>();
            foreach (var worldId in sortedWorldIds)
            {
                // Already Unlocked.
                if (unlockedIds.Contains(worldId))
                {
                    throw new AlreadyWorldUnlockedException($"World {worldId} Already unlocked.");
                }

                WorldUnlockSheet.Row row =
                    worldUnlockSheet.OrderedList.First(r => r.WorldIdToUnlock == worldId);
                // Check Previous world unlocked.
                if (!worldInformation.IsWorldUnlocked(row.WorldId) || !unlockedIds.Contains(row.WorldId))
                {
                    throw new FailedToUnlockWorldException($"unlock ${row.WorldId} first.");
                }

                // Check stage cleared in HackAndSlash.
                if (!worldInformation.IsWorldUnlocked(worldId) && !worldInformation.IsStageCleared(row.StageId))
                {
                    throw new FailedToUnlockWorldException($"{worldId} is locked.");
                }

                unlockedIds.Add(worldId);
            }

            FungibleAssetValue cost =
                CrystalCalculator.CalculateWorldUnlockCost(sortedWorldIds, worldUnlockSheet);
            FungibleAssetValue balance = states.GetBalance(context.Signer, cost.Currency);

            // Insufficient CRYSTAL.
            if (balance < cost)
            {
                throw new NotEnoughFungibleAssetValueException($"UnlockWorld required {cost}, but balance is {balance}");
            }

            if (migrationRequired)
            {
                states = states
                    .SetState(AvatarAddress, avatarState.SerializeV2())
                    .SetState(questListAddress, avatarState.questList.Serialize())
                    .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                    .SetState(inventoryAddress, avatarState.inventory.Serialize());
            }

            return states
                .SetState(unlockedWorldIdsAddress, new List(unlockedIds.Select(i => i.Serialize())))
                .TransferAsset(context.Signer, Addresses.UnlockWorld, cost);
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
            => new Dictionary<string, IValue>
            {
                ["w"] = new List(WorldIds.Select(i => i.Serialize())),
                ["a"] = AvatarAddress.Serialize(),
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            WorldIds = plainValue["w"].ToList(StateExtensions.ToInteger);
            AvatarAddress = plainValue["a"].ToAddress();
        }
    }
}
