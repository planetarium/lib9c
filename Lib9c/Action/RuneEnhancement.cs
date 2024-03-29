using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Rune;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("runeEnhancement3")]
    public class RuneEnhancement : GameAction, IRuneEnhancementV1
    {
        public Address AvatarAddress;
        public int RuneId;
        public int TryCount = 1;

        Address IRuneEnhancementV1.AvatarAddress => AvatarAddress;
        int IRuneEnhancementV1.RuneId => RuneId;
        int IRuneEnhancementV1.TryCount => TryCount;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["r"] = RuneId.Serialize(),
                ["t"] = TryCount.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            RuneId = plainValue["r"].ToInteger();
            TryCount = plainValue["t"].ToInteger();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;

            if (!states.TryGetAvatarState(context.Signer, AvatarAddress, out _))
            {
                throw new FailedLoadStateException(
                    $"Aborted as the avatar state of the signer was failed to load.");
            }

            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(ArenaSheet),
                    typeof(RuneSheet),
                    typeof(RuneListSheet),
                    typeof(RuneCostSheet),
                });

            // Validation
            if (TryCount < 1)
            {
                throw new TryCountIsZeroException(
                    $"{AvatarAddress}TryCount must be greater than 0. " +
                    $"current TryCount : {TryCount}");
            }

            var runeStateAddress = RuneState.DeriveAddress(AvatarAddress, RuneId);
            var runeState = states.TryGetLegacyState(runeStateAddress, out List rawState)
                ? new RuneState(rawState)
                : new RuneState(RuneId);

            var costSheet = sheets.GetSheet<RuneCostSheet>();
            if (!costSheet.TryGetValue(runeState.RuneId, out var costRow))
            {
                throw new RuneCostNotFoundException(
                    $"[{nameof(RuneEnhancement)}] my avatar address : {AvatarAddress}");
            }

            var targetLevel = runeState.Level + TryCount;
            if (!costRow.TryGetCost(targetLevel, out var cost))
            {
                throw new RuneCostNotFoundException(
                    $"[{nameof(RuneEnhancement)}] my avatar address : {AvatarAddress} : Maybe max level reached");
            }

            var runeSheet = sheets.GetSheet<RuneSheet>();
            if (!runeSheet.TryGetValue(runeState.RuneId, out var runeRow))
            {
                throw new RuneNotFoundException(
                    $"[{nameof(RuneEnhancement)}] my avatar address : {AvatarAddress}");
            }

            var ncgCurrency = states.GetGoldCurrency();
            var crystalCurrency = CrystalCalculator.CRYSTAL;
            var runeCurrency = Currency.Legacy(runeRow.Ticker, 0, minters: null);
            var ncgBalance = states.GetBalance(context.Signer, ncgCurrency);
            var crystalBalance = states.GetBalance(context.Signer, crystalCurrency);
            var runeBalance = states.GetBalance(AvatarAddress, runeCurrency);
            var random = context.GetRandom();
            if (!RuneHelper.TryEnhancement(runeState.Level, costRow, random, TryCount,
                    out var levelUpResult))
            {
                // Rune cost not found while level up
                throw new RuneCostNotFoundException(
                    $"[{nameof(RuneEnhancement)}] my avatar address : {AvatarAddress} : Maybe max level reached");
            }

            // Check final balance
            if (ncgBalance < levelUpResult.ncgCost * ncgCurrency ||
                crystalBalance < levelUpResult.crystalCost * crystalCurrency ||
                runeBalance < levelUpResult.runeCost * runeCurrency)
            {
                throw new NotEnoughFungibleAssetValueException(
                    $"{nameof(RuneEnhancement)}" +
                    $"[ncg:{ncgBalance} < {levelUpResult.ncgCost * ncgCurrency}] " +
                    $"[crystal:{crystalBalance} < {levelUpResult.crystalCost * crystalCurrency}] " +
                    $"[rune:{runeBalance} < {levelUpResult.runeCost * runeCurrency}]"
                );
            }

            runeState.LevelUp(levelUpResult.levelUpCount);
            states = states.SetLegacyState(runeStateAddress, runeState.Serialize());

            var arenaSheet = sheets.GetSheet<ArenaSheet>();
            var arenaData = arenaSheet.GetRoundByBlockIndex(context.BlockIndex);
            var feeStoreAddress = Addresses.GetBlacksmithFeeAddress(arenaData.ChampionshipId, arenaData.Round);

            // Burn costs
            if (levelUpResult.ncgCost > 0)
            {
                states = states.TransferAsset(context, context.Signer, feeStoreAddress,
                    levelUpResult.ncgCost * ncgCurrency);
            }

            if (levelUpResult.crystalCost > 0)
            {
                states = states.TransferAsset(context, context.Signer, feeStoreAddress,
                    levelUpResult.crystalCost * crystalCurrency);
            }

            if (levelUpResult.runeCost > 0)
            {
                states = states.TransferAsset(context, AvatarAddress, feeStoreAddress,
                    levelUpResult.runeCost * runeCurrency);
            }

            return states;
        }
    }
}
