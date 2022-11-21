using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Action.Interface;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Rune;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/1502
    /// </summary>
    [Serializable]
    [ActionType("runeEnhancement01")]
    public class RuneEnhancement01 : GameAction, IRuneEnhancement
    {
        public const int Version = 1;

        // NOTE:
        // Current block index of main-net is 5_403_245(Mon Nov 14 2022 17:28:17 GMT+0900).
        // Target release date of v100340 is Dec 14 2022 10:00:00 GMT+0900.
        public const long AvailableBlockIndex = 5_617_000;

        public Address AvatarAddress { get; set; }
        public int RuneId { get; set; }
        public int TryCount { get; set; } = 1;

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

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            // NOTE: This action is available since `AvailableBlockIndex`.
            CheckActionAvailable(AvailableBlockIndex - 1, context);

            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(ArenaSheet),
                    typeof(RuneSheet),
                    typeof(RuneListSheet),
                    typeof(RuneCostSheet),
                });

            if (TryCount < 1)
            {
                throw new TryCountIsZeroException(
                    $"{AvatarAddress}TryCount must be greater than 0. " +
                    $"current TryCount : {TryCount}");
            }

            RuneState runeState;
            var runeStateAddress = RuneState.DeriveAddress(AvatarAddress, RuneId);
            if (states.TryGetState(runeStateAddress, out List rawState))
            {
                runeState = new RuneState(rawState);
            }
            else
            {
                runeState = new RuneState(RuneId);
            }

            var costSheet = sheets.GetSheet<RuneCostSheet>();
            if (!costSheet.TryGetValue(runeState.RuneId, out var costRow))
            {
                throw new RuneCostNotFoundException(
                    $"[{nameof(RuneEnhancement01)}] my avatar address : {AvatarAddress}");
            }

            var targetLevel = runeState.Level + 1;
            if (!costRow.TryGetCost(targetLevel, out var cost))
            {
                throw new RuneCostDataNotFoundException(
                    $"[{nameof(RuneEnhancement01)}] my avatar address : {AvatarAddress}");
            }

            var runeSheet = sheets.GetSheet<RuneSheet>();
            if (!runeSheet.TryGetValue(runeState.RuneId, out var runeRow))
            {
                throw new RuneNotFoundException(
                    $"[{nameof(RuneEnhancement01)}] my avatar address : {AvatarAddress}");
            }

            var ncgCurrency = states.GetGoldCurrency();
            var crystalCurrency = CrystalCalculator.CRYSTAL;
            var runeCurrency = Currency.Legacy(runeRow.Ticker, 0, minters: null);
            var ncgBalance = states.GetBalance(context.Signer, ncgCurrency);
            var crystalBalance = states.GetBalance(context.Signer, crystalCurrency);
            var runeBalance = states.GetBalance(AvatarAddress, runeCurrency);
            if (RuneHelper.TryEnhancement(ncgBalance, crystalBalance, runeBalance,
                    ncgCurrency, crystalCurrency, runeCurrency,
                    cost, context.Random, TryCount, out var tryCount))
            {
                runeState.LevelUp();
                states = states.SetState(runeStateAddress, runeState.Serialize());
            }

            // update rune slot
            for (var i = 1; i < (int)BattleType.End; i++)
            {
                var runeSlotStateAddress = RuneSlotState.DeriveAddress(AvatarAddress, (BattleType)i);
                if (states.TryGetState(runeSlotStateAddress, out List rawRuneSlotState))
                {
                    var runeSlotState = new RuneSlotState(rawRuneSlotState);
                    runeSlotState.UpdateSlotItem(runeState);
                    states = states.SetState(runeSlotStateAddress, runeSlotState.Serialize());
                }
            }

            var arenaSheet = sheets.GetSheet<ArenaSheet>();
            var arenaData = arenaSheet.GetRoundByBlockIndex(context.BlockIndex);
            var feeStoreAddress = Addresses.GetBlacksmithFeeAddress(arenaData.ChampionshipId, arenaData.Round);

            var ncgCost = cost.NcgQuantity * tryCount * ncgCurrency;
            if (cost.NcgQuantity > 0)
            {
                states = states.TransferAsset(context.Signer, feeStoreAddress, ncgCost);
            }

            var crystalCost = cost.CrystalQuantity * tryCount * crystalCurrency;
            if (cost.CrystalQuantity > 0)
            {
                states = states.TransferAsset(context.Signer, feeStoreAddress, crystalCost);
            }

            var runeCost = cost.RuneStoneQuantity * tryCount * runeCurrency;
            if (cost.RuneStoneQuantity > 0)
            {
                states = states.TransferAsset(AvatarAddress, feeStoreAddress, runeCost);
            }

            return states;
        }
    }
}
