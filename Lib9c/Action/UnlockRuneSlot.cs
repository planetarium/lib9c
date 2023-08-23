using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Rune;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("unlock_rune_slot")]
    public class UnlockRuneSlot : GameAction, IUnlockRuneSlotV1
    {
        public Address AvatarAddress;
        public int SlotIndex;

        Address IUnlockRuneSlotV1.AvatarAddress => AvatarAddress;
        int IUnlockRuneSlotV1.SlotIndex => SlotIndex;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
                {
                    ["a"] = AvatarAddress.Serialize(),
                    ["s"] = SlotIndex.Serialize(),
                }
                .ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            SlotIndex = plainValue["s"].ToInteger();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            if (context.Rehearsal)
            {
                return context.PreviousState;
            }

            var world = context.PreviousState;
            var sheets = LegacyModule.GetSheets(
                world,
                sheetTypes: new[]
                {
                    typeof(ArenaSheet),
                });

            var adventureSlotStateAddress =
                RuneSlotState.DeriveAddress(AvatarAddress, BattleType.Adventure);
            var adventureSlotState = LegacyModule.TryGetState(
                world,
                adventureSlotStateAddress,
                out List rawAdventureSlotState)
                ? new RuneSlotState(rawAdventureSlotState)
                : new RuneSlotState(BattleType.Adventure);


            var arenaSlotStateAddress =
                RuneSlotState.DeriveAddress(AvatarAddress, BattleType.Arena);
            var arenaSlotState = LegacyModule.TryGetState(
                world,
                arenaSlotStateAddress,
                out List rawArenaSlotState)
                ? new RuneSlotState(rawArenaSlotState)
                : new RuneSlotState(BattleType.Arena);

            var raidSlotStateAddress = RuneSlotState.DeriveAddress(AvatarAddress, BattleType.Raid);
            var raidSlotState = LegacyModule.TryGetState(
                world,
                raidSlotStateAddress,
                out List rawRaidSlotState)
                ? new RuneSlotState(rawRaidSlotState)
                : new RuneSlotState(BattleType.Raid);


            var slot = adventureSlotState.GetRuneSlot().FirstOrDefault(x => x.Index == SlotIndex);
            if (slot == null)
            {
                throw new SlotNotFoundException(
                    $"[{nameof(UnlockRuneSlot)}] Index : {SlotIndex}");
            }

            // note : You will need to modify it later when applying staking unlock.
            if (slot.RuneSlotType != RuneSlotType.Ncg)
            {
                throw new MismatchRuneSlotTypeException(
                    $"[{nameof(UnlockRuneSlot)}] RuneSlotType : {slot.RuneSlotType}");
            }

            var gameConfigState = LegacyModule.GetGameConfigState(world);
            var cost = slot.RuneType == RuneType.Stat
                ? gameConfigState.RuneStatSlotUnlockCost
                : gameConfigState.RuneSkillSlotUnlockCost;
            var ncgCurrency = LegacyModule.GetGoldCurrency(world);
            var arenaSheet = sheets.GetSheet<ArenaSheet>();
            var arenaData = arenaSheet.GetRoundByBlockIndex(context.BlockIndex);
            var feeStoreAddress = Addresses.GetBlacksmithFeeAddress(
                arenaData.ChampionshipId,
                arenaData.Round);

            adventureSlotState.Unlock(SlotIndex);
            arenaSlotState.Unlock(SlotIndex);
            raidSlotState.Unlock(SlotIndex);

            world = LegacyModule.TransferAsset(
                world,
                context,
                context.Signer,
                feeStoreAddress,
                cost * ncgCurrency);
            world = LegacyModule.SetState(
                world,
                adventureSlotStateAddress,
                adventureSlotState.Serialize());
            world = LegacyModule.SetState(world, arenaSlotStateAddress, arenaSlotState.Serialize());
            world = LegacyModule.SetState(world, raidSlotStateAddress, raidSlotState.Serialize());
            return world;
        }
    }
}
