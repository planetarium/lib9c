using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Nekoyume.Arena;
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
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(ArenaSheet),
                });

            var adventureSlotStateAddress = RuneSlotState.DeriveAddress(AvatarAddress, BattleType.Adventure);
            var adventureSlotState = states.TryGetLegacyState(adventureSlotStateAddress, out List rawAdventureSlotState)
                ? new RuneSlotState(rawAdventureSlotState)
                : new RuneSlotState(BattleType.Adventure);


            var arenaSlotStateAddress = RuneSlotState.DeriveAddress(AvatarAddress, BattleType.Arena);
            var arenaSlotState = states.TryGetLegacyState(arenaSlotStateAddress, out List rawArenaSlotState)
                ? new RuneSlotState(rawArenaSlotState)
                : new RuneSlotState(BattleType.Arena);

            var raidSlotStateAddress = RuneSlotState.DeriveAddress(AvatarAddress, BattleType.Raid);
            var raidSlotState = states.TryGetLegacyState(raidSlotStateAddress, out List rawRaidSlotState)
                ? new RuneSlotState(rawRaidSlotState)
                : new RuneSlotState(BattleType.Raid);

            var infiniteTowerSlotStateAddress = RuneSlotState.DeriveAddress(AvatarAddress, BattleType.InfiniteTower);
            var infiniteTowerSlotState = states.TryGetLegacyState(infiniteTowerSlotStateAddress, out List rawInfiniteTowerSlotState)
                ? new RuneSlotState(rawInfiniteTowerSlotState)
                : new RuneSlotState(BattleType.InfiniteTower);

            var slot = adventureSlotState.GetRuneSlot().FirstOrDefault(x => x.Index == SlotIndex);
            if (slot == null)
            {
                throw new SlotNotFoundException(
                    $"[{nameof(UnlockRuneSlot)}] Index : {SlotIndex}");
            }

            var gameConfigState = states.GetGameConfigState();
            int cost;
            Currency currency;
            switch (slot.RuneSlotType)
            {
                case RuneSlotType.Ncg:
                    cost = slot.RuneType == RuneType.Stat
                        ? gameConfigState.RuneStatSlotUnlockCost
                        : gameConfigState.RuneSkillSlotUnlockCost;
                    currency = states.GetGoldCurrency();
                    break;
                case RuneSlotType.Crystal:
                    cost = slot.RuneType == RuneType.Stat
                        ? gameConfigState.RuneStatSlotCrystalUnlockCost
                        : gameConfigState.RuneSkillSlotCrystalUnlockCost;
                    currency = Currencies.Crystal;
                    break;
                default:
                    throw new MismatchRuneSlotTypeException(
                        $"[{nameof(UnlockRuneSlot)}] RuneSlotType : {slot.RuneSlotType}");
            }

            adventureSlotState.Unlock(SlotIndex);
            arenaSlotState.Unlock(SlotIndex);
            raidSlotState.Unlock(SlotIndex);
            infiniteTowerSlotState.Unlock(SlotIndex);

            var feeAddress = states.GetFeeAddress(context.BlockIndex);

            return states
                .TransferAsset(context, context.Signer, feeAddress, cost * currency)
                .SetLegacyState(adventureSlotStateAddress, adventureSlotState.Serialize())
                .SetLegacyState(arenaSlotStateAddress, arenaSlotState.Serialize())
                .SetLegacyState(raidSlotStateAddress, raidSlotState.Serialize())
                .SetLegacyState(infiniteTowerSlotStateAddress, infiniteTowerSlotState.Serialize());
        }
    }
}
