using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Nekoyume.Arena;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Rune;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Rune;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("runeEnhancement2")]
    public class RuneEnhancement : GameAction, IRuneEnhancementV1
    {
        public Address AvatarAddress;
        public int RuneId;
        public int TryCount = 1;

        public struct LevelUpResult
        {
            public int LevelUpCount { get; set; }
            public int NcgCost { get; set; }
            public int CrystalCost { get; set; }
            public int RuneCost { get; set; }

            public override string ToString() =>
                $"{LevelUpCount} level up with cost {NcgCost} NCG, {CrystalCost} Crystal, {RuneCost} Runestone.";
        }

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
            GasTracer.UseGas(1);
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
                    typeof(RuneLevelBonusSheet),
                });

            // Validation
            if (TryCount < 1)
            {
                throw new TryCountIsZeroException(
                    $"{AvatarAddress}TryCount must be greater than 0. " +
                    $"current TryCount : {TryCount}");
            }

            var allRuneState = states.GetRuneState(AvatarAddress, out _);

            RuneState runeState;
            if (allRuneState.TryGetRuneState(RuneId, out var rs))
            {
                runeState = rs;
            }
            else
            {
                runeState = new RuneState(RuneId);
                allRuneState.AddRuneState(runeState);
            }

            var costSheet = sheets.GetSheet<RuneCostSheet>();
            if (!costSheet.TryGetValue(runeState.RuneId, out var costRow))
            {
                throw new RuneCostNotFoundException(
                    $"[{nameof(RuneEnhancement)}] my avatar address : {AvatarAddress}");
            }

            var targetLevel = runeState.Level + TryCount;
            if (!costRow.TryGetCost(targetLevel, out _))
            {
                throw new RuneCostDataNotFoundException(
                    $"[{nameof(RuneEnhancement)}] my avatar address : {AvatarAddress} : Maybe max level reached");
            }

            var runeSheet = sheets.GetSheet<RuneSheet>();
            if (!runeSheet.TryGetValue(runeState.RuneId, out var runeRow))
            {
                throw new RuneNotFoundException(
                    $"[{nameof(RuneEnhancement)}] my avatar address : {AvatarAddress}");
            }

            var random = context.GetRandom();
            if (!RuneHelper.TryEnhancement(runeState.Level, costRow, random, TryCount,
                    out var levelUpResult))
            {
                // Rune cost not found while level up
                throw new RuneCostDataNotFoundException(
                    $"[{nameof(RuneEnhancement)}] my avatar address : {AvatarAddress} : Maybe max level reached");
            }

            // Check final balance
            var ncgCurrency = states.GetGoldCurrency();
            var crystalCurrency = CrystalCalculator.CRYSTAL;
            var runeCurrency = Currency.Legacy(runeRow.Ticker, 0, minters: null);
            var ncgBalance = states.GetBalance(context.Signer, ncgCurrency);
            var crystalBalance = states.GetBalance(context.Signer, crystalCurrency);
            var runeBalance = states.GetBalance(AvatarAddress, runeCurrency);

            if (ncgBalance < levelUpResult.NcgCost * ncgCurrency ||
                crystalBalance < levelUpResult.CrystalCost * crystalCurrency ||
                runeBalance < levelUpResult.RuneCost * runeCurrency)
            {
                throw new NotEnoughFungibleAssetValueException(
                    $"{nameof(RuneEnhancement)}" +
                    $"[ncg:{ncgBalance} < {levelUpResult.NcgCost * ncgCurrency}] " +
                    $"[crystal:{crystalBalance} < {levelUpResult.CrystalCost * crystalCurrency}] " +
                    $"[rune:{runeBalance} < {levelUpResult.RuneCost * runeCurrency}]"
                );
            }

            runeState.LevelUp(levelUpResult.LevelUpCount);
            states = states.SetRuneState(AvatarAddress, allRuneState);

            var feeAddress = Addresses.RewardPool;
            // TODO: [GuildMigration] Remove this after migration
            if (states.GetDelegationMigrationHeight() is long migrationHeight
                && context.BlockIndex < migrationHeight)
            {
                var arenaSheet = states.GetSheet<ArenaSheet>();
                var arenaData = arenaSheet.GetRoundByBlockIndex(context.BlockIndex);
                feeAddress = ArenaHelper.DeriveArenaAddress(arenaData.ChampionshipId, arenaData.Round);
            }

            // Burn costs
            if (levelUpResult.NcgCost > 0)
            {
                states = states.TransferAsset(context, context.Signer, feeAddress,
                    levelUpResult.NcgCost * ncgCurrency);
            }

            if (levelUpResult.CrystalCost > 0)
            {
                states = states.TransferAsset(context, context.Signer, feeAddress,
                    levelUpResult.CrystalCost * crystalCurrency);
            }

            if (levelUpResult.RuneCost > 0)
            {
                states = states.TransferAsset(context, AvatarAddress, feeAddress,
                    levelUpResult.RuneCost * runeCurrency);
            }

            return states;
        }
    }
}
