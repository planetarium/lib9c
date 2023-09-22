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
using Nekoyume.Action.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1309
    /// </summary>
    [ActionType("unlock_world2")]
    public class UnlockWorld: GameAction, IUnlockWorldV1
    {
        public List<int> WorldIds;
        public Address AvatarAddress;

        IEnumerable<int> IUnlockWorldV1.WorldIds => WorldIds;
        Address IUnlockWorldV1.AvatarAddress => AvatarAddress;

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var world = context.PreviousState;
            var unlockedWorldIdsAddress = AvatarAddress.Derive("world_ids");
            if (context.Rehearsal)
            {
                world = AvatarModule.MarkChanged(world, AvatarAddress, true, true, true, true);
                world = LegacyModule.MarkBalanceChanged(
                    world,
                    context,
                    GoldCurrencyMock,
                    context.Signer,
                    Addresses.UnlockWorld);
                return world;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}UnlockWorld exec started", addressesHex);
            if (!WorldIds.Any() || WorldIds.Any(i => i < 2 || i == GameConfig.MimisbrunnrWorldId))
            {
                throw new InvalidWorldException();
            }

            WorldInformation worldInformation;
            AvatarState avatarState = null;
            bool migrationRequired = false;

            if (AvatarModule.GetWorldInformation(world, AvatarAddress) is { } worldInfo)
            {
                worldInformation = worldInfo;
            }
            else if (LegacyModule.TryGetState(
                    world,
                    AvatarAddress.Derive(LegacyWorldInformationKey),
                    out Dictionary rawInfo))
            {
                // AvatarState migration required (v1 -> v2).
                worldInformation = new WorldInformation(rawInfo);
                migrationRequired = true;
            }
            else
            {
                // AvatarState migration required (v0 -> v2).
                if (AvatarModule.TryGetAvatarState(
                        world,
                        context.Signer,
                        AvatarAddress,
                        out avatarState))
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

            List<int> unlockedIds = LegacyModule.TryGetState(
                world,
                unlockedWorldIdsAddress,
                out List rawIds)
                ? rawIds.ToList(StateExtensions.ToInteger)
                : new List<int>
                {
                    1,
                    GameConfig.MimisbrunnrWorldId,
                };

            var sortedWorldIds = WorldIds.OrderBy(i => i).ToList();
            var worldUnlockSheet = LegacyModule.GetSheet<WorldUnlockSheet>(world);
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
                if (!worldInformation.IsWorldUnlocked(row.WorldId) ||
                    !unlockedIds.Contains(row.WorldId))
                {
                    throw new FailedToUnlockWorldException($"unlock ${row.WorldId} first.");
                }

                // Check stage cleared in HackAndSlash.
                // If world is unlocked or can unlock, Execute it.
                if (!worldInformation.IsWorldUnlocked(worldId) &&
                    !worldInformation.IsStageCleared(row.StageId))
                {
                    throw new FailedToUnlockWorldException($"{worldId} is locked.");
                }

                unlockedIds.Add(worldId);
            }

            FungibleAssetValue cost =
                CrystalCalculator.CalculateWorldUnlockCost(sortedWorldIds, worldUnlockSheet);
            FungibleAssetValue balance = LegacyModule.GetBalance(
                world,
                context.Signer,
                cost.Currency);

            // Insufficient CRYSTAL.
            if (balance < cost)
            {
                throw new NotEnoughFungibleAssetValueException(
                    $"UnlockWorld required {cost}, but balance is {balance}");
            }

            if (migrationRequired)
            {
                world = AvatarModule.SetAvatarState(
                    world,
                    AvatarAddress,
                    avatarState,
                    true,
                    true,
                    true,
                    true);
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug(
                "{AddressesHex}UnlockWorld Total Executed Time: {Elapsed}",
                addressesHex,
                ended - started);
            world = LegacyModule.SetState(
                world,
                unlockedWorldIdsAddress,
                new List(unlockedIds.Select(i => i.Serialize())));
            world = LegacyModule.TransferAsset(
                world,
                context,
                context.Signer,
                Addresses.UnlockWorld,
                cost);
            return world;
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
