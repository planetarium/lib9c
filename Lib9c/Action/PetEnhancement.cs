using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Nekoyume.Arena;
using Nekoyume.Exceptions;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType(ActionTypeIdentifier)]
    public class PetEnhancement : GameAction
    {
        public const string ActionTypeIdentifier = "pet_enhancement";

        public Address AvatarAddress;
        public int PetId;
        public int TargetLevel;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["p"] = PetId.Serialize(),
                ["t"] = TargetLevel.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            PetId = plainValue["p"].ToInteger();
            TargetLevel = plainValue["t"].ToInteger();
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;
            var addresses = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            // NOTE: The `AvatarAddress` must contained in `Signer`'s `AgentState.avatarAddresses`.
            if (!Addresses.CheckAvatarAddrIsContainedInAgent(context.Signer, AvatarAddress))
            {
                throw new InvalidActionFieldException(
                    ActionTypeIdentifier,
                    addresses,
                    nameof(AvatarAddress),
                    $"Signer({context.Signer}) is not contained in" +
                    $" AvatarAddress({AvatarAddress}).");
            }

            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(ArenaSheet),
                    typeof(PetSheet),
                    typeof(PetCostSheet),
                });

            if (TargetLevel < 1)
            {
                throw new InvalidActionFieldException(
                    ActionTypeIdentifier,
                    addresses,
                    nameof(TargetLevel),
                    $"TargetLevel({TargetLevel}) must be greater than 0.");
            }

            var petStateAddress = PetState.DeriveAddress(AvatarAddress, PetId);
            var petState = states.TryGetLegacyState(petStateAddress, out List rawState)
                ? new PetState(rawState)
                : new PetState(PetId);
            if (TargetLevel <= petState.Level)
            {
                throw new InvalidActionFieldException(
                    ActionTypeIdentifier,
                    addresses,
                    nameof(TargetLevel),
                    $"TargetLevel({TargetLevel}) must be greater than" +
                    $" current pet level({petState.Level}).");
            }

            var petSheet = sheets.GetSheet<PetSheet>();
            if (!petSheet.TryGetValue(petState.PetId, out var petRow))
            {
                throw new SheetRowNotFoundException(
                    ActionTypeIdentifier,
                    addresses,
                    nameof(PetSheet),
                    PetId);
            }

            var costSheet = sheets.GetSheet<PetCostSheet>();
            if (!costSheet.TryGetValue(petState.PetId, out var costRow))
            {
                throw new SheetRowNotFoundException(
                    ActionTypeIdentifier,
                    addresses,
                    nameof(PetCostSheet),
                    petState.PetId);
            }

            if (!costRow.TryGetCost(TargetLevel, out _))
            {
                // cost not found with level
                throw new PetCostNotFoundException(
                    ActionTypeIdentifier,
                    addresses,
                    $"Can not find cost by TargetLevel({TargetLevel}).");
            }

            var ncgCurrency = states.GetGoldCurrency();
            var soulStoneCurrency = PetHelper.GetSoulstoneCurrency(petRow.SoulStoneTicker);
            var (ncgQuantity, soulStoneQuantity) = PetHelper.CalculateEnhancementCost(
                costSheet,
                PetId,
                petState.Level,
                TargetLevel);

            if (ncgQuantity > 0)
            {
                var ncgCost = ncgQuantity * ncgCurrency;
                var currentNcg = states.GetBalance(context.Signer, ncgCurrency);
                if (currentNcg < ncgCost)
                {
                    throw new NotEnoughFungibleAssetValueException(
                        ActionTypeIdentifier,
                        GetSignerAndOtherAddressesHex(context),
                        ncgCost,
                        currentNcg);
                }

                var feeAddress = states.GetFeeAddress(context.BlockIndex);

                states = states.TransferAsset(context, context.Signer, feeAddress, ncgCost);
            }

            if (soulStoneQuantity > 0)
            {
                var soulStoneCost = soulStoneQuantity * soulStoneCurrency;
                var currentSoulStone = states.GetBalance(
                    AvatarAddress,
                    soulStoneCurrency);
                if (currentSoulStone < soulStoneCost)
                {
                    throw new NotEnoughFungibleAssetValueException(
                        ActionTypeIdentifier,
                        GetSignerAndOtherAddressesHex(context, AvatarAddress),
                        soulStoneCost,
                        currentSoulStone);
                }

                states = states.TransferAsset(
                    context,
                    AvatarAddress,
                    Addresses.RewardPool,
                    soulStoneCost);
            }

            while (petState.Level < TargetLevel)
            {
                petState.LevelUp();
            }

            return states.SetLegacyState(petStateAddress, petState.Serialize());
        }
    }
}
