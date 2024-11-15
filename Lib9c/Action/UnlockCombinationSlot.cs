using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Arena;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class UnlockCombinationSlot : GameAction
    {
        private const string TypeIdentifier = "unlock_combination_slot";

        // TODO: 별도 파일로 분리
        public const int GoldenDustId = 600201;
        public const int RubyDustId = 600202;

        public Address AvatarAddress;
        public int SlotIndex;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
                {
                    ["a"] = AvatarAddress.Serialize(),
                    ["s"] = (Integer)SlotIndex,
                }
                .ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            SlotIndex = (Integer)plainValue["s"];
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(MaterialItemSheet),
                    typeof(UnlockCombinationSlotCostSheet),
                });

            if (!CombinationSlotState.ValidateSlotIndex(SlotIndex))
            {
                throw new InvalidSlotIndexException($"[{nameof(UnlockRuneSlot)}] Index : {SlotIndex}");
            }

            var allSlotState = states.GetAllCombinationSlotState(AvatarAddress);
            var hasSlot = allSlotState.TryGetSlot(SlotIndex, out var combinationSlot);
            if (!hasSlot || combinationSlot is null)
            {
                combinationSlot = new CombinationSlotState(
                    CombinationSlotState.DeriveAddress(AvatarAddress, SlotIndex),
                    SlotIndex
                );
            }

            if (combinationSlot.IsUnlocked)
            {
                throw new SlotAlreadyUnlockedException($"[{nameof(UnlockRuneSlot)}] Index : {SlotIndex}");
            }

            var feeStoreAddress = GetFeeStoreAddress(states, context.BlockIndex);
            var costSheet = sheets.GetSheet<UnlockCombinationSlotCostSheet>();

            if (!costSheet.ContainsKey(SlotIndex))
            {
                throw new InvalidSlotIndexException($"[{nameof(UnlockRuneSlot)}] Index On Sheet : {SlotIndex}");
            }

            var price = costSheet[SlotIndex];
            var avatarState = states.GetAvatarState(AvatarAddress, true, false, false);
            if (avatarState is null || !avatarState.agentAddress.Equals(context.Signer))
            {
                var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }
            
            var agentAddress = avatarState.agentAddress;
            
            var useMaterial = false;

            MaterialItemSheet materialSheet = null;
            Inventory inventory = null;

            // Use Crystal
            if (price.CrystalPrice > 0)
            {
                var currency = Currencies.Crystal;
                var balance = states.GetBalance(agentAddress, currency);
                var crystalPrice = price.CrystalPrice * currency;

                if (balance < price.CrystalPrice * currency)
                {
                    throw new InsufficientBalanceException($"{balance} is less than {crystalPrice}",
                        agentAddress, balance
                    );
                }

                states = states.TransferAsset(context, agentAddress, feeStoreAddress, crystalPrice);
            }

            // Use GoldenDust
            if (price.GoldenDustPrice > 0)
            {
                materialSheet = sheets.GetSheet<MaterialItemSheet>();
                var material = materialSheet.OrderedList.First(m => m.Id == GoldenDustId);

                inventory = states.GetInventoryV2(AvatarAddress);
                if (!inventory.RemoveFungibleItem(material.ItemId, context.BlockIndex,
                    price.GoldenDustPrice))
                {
                    throw new NotEnoughMaterialException(
                        $"Not enough golden dust to open slot: needs {price.GoldenDustPrice}");
                }

                useMaterial = true;
            }

            // Use RubyDust
            if (price.RubyDustPrice > 0)
            {
                materialSheet ??= sheets.GetSheet<MaterialItemSheet>();
                var material = materialSheet.OrderedList.First(m => m.Id == RubyDustId);

                inventory ??= states.GetInventoryV2(AvatarAddress);
                if (!inventory.RemoveFungibleItem(material.ItemId, context.BlockIndex,
                    price.RubyDustPrice))
                {
                    throw new NotEnoughMaterialException(
                        $"Not enough ruby dust to open slot: needs {price.RubyDustPrice}");
                }

                useMaterial = true;
            }

            // Use NCG
            if (price.NcgPrice > 0)
            {
                var currency = states.GetGoldCurrency();
                var balance = states.GetBalance(agentAddress, currency);
                var ncgPrice = price.NcgPrice * currency;

                if (balance < price.NcgPrice * currency)
                {
                    throw new InsufficientBalanceException($"{balance} is less than {ncgPrice}",
                        agentAddress, balance
                    );
                }

                states = states.TransferAsset(context, agentAddress, feeStoreAddress, ncgPrice);
            }

            // useMaterial이 true일 경우에만 inventory를 업데이트한다.
            if (useMaterial)
            {
                states = states.SetInventory(AvatarAddress, inventory);
            }

            allSlotState.UnlockSlot(AvatarAddress, SlotIndex);
            return states.SetCombinationSlotState(AvatarAddress, allSlotState);
        }

        private Address GetFeeStoreAddress(IWorld states, long blockIndex)
        {
            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(ArenaSheet),
                });

            var arenaSheet = sheets.GetSheet<ArenaSheet>();
            var arenaData = arenaSheet.GetRoundByBlockIndex(blockIndex);
            return ArenaHelper.DeriveArenaAddress(arenaData.ChampionshipId, arenaData.Round);
        }
    }
}
