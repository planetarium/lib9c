using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Exceptions;
using Nekoyume.Action.Guild.Migration.LegacyModels;
using Nekoyume.Arena;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Nekoyume.TableData.Rune;
using Nekoyume.TableData.Summon;
using Serilog;

namespace Nekoyume.Action
{
    [ActionType("rune_summon")]
    public class RuneSummon : GameAction, IRuneSummonV1
    {
        public const string AvatarAddressKey = "aa";
        public Address AvatarAddress;

        public const string GroupIdKey = "gid";
        public int GroupId;

        public const string SummonCountKey = "sc";
        public int SummonCount;

        /// <summary>
        /// Number of runes obtained per summon.
        /// </summary>
        public const int RuneQuantity = 10;

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug($"{addressesHex} RuneSummon Exec. Started.");

            if (!states.TryGetAvatarState(context.Signer, AvatarAddress, out var avatarState))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex} Aborted as the avatar state of the signer was failed to load.");
            }

            if (!SummonHelper.CheckSummonCountIsValid(SummonCount))
            {
                throw new InvalidSummonCountException(
                    $"{addressesHex} Given summonCount {SummonCount} is not valid. Please use 1 or 10 or 100"
                );
            }

            // Validate Work
            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(sheetTypes: new[]
            {
                typeof(RuneSummonSheet),
                typeof(MaterialItemSheet),
                typeof(RuneSheet),
            });

            var summonSheet = sheets.GetSheet<RuneSummonSheet>();
            var materialSheet = sheets.GetSheet<MaterialItemSheet>();
            var runeSheet = sheets.GetSheet<RuneSheet>();

            var summonRow = summonSheet.OrderedList.FirstOrDefault(row => row.GroupId == GroupId);
            if (summonRow is null)
            {
                throw new RowNotInTableException(
                    $"{addressesHex} Failed to get {GroupId} in RuneSummonSheet");
            }

            // Use materials
            var inventory = avatarState.inventory;
            var material = materialSheet.OrderedList.First(m => m.Id == summonRow.CostMaterial);
            if (!inventory.RemoveFungibleItem(material.ItemId, context.BlockIndex,
                    summonRow.CostMaterialCount * SummonCount))
            {
                throw new NotEnoughMaterialException(
                    $"{addressesHex} Aborted as the player has no enough material ({summonRow.CostMaterial} * {summonRow.CostMaterialCount})");
            }

            // Transfer Cost NCG first for fast-fail
            if (summonRow.CostNcg > 0L)
            {
                var feeAddress = states.GetFeeAddress(context.BlockIndex);

                states = states.TransferAsset(
                    context,
                    context.Signer,
                    feeAddress,
                    states.GetGoldCurrency() * summonRow.CostNcg * SummonCount
                );
            }

            var random = context.GetRandom();
            states = Summon(
                context,
                AvatarAddress,
                runeSheet,
                summonRow,
                SummonCount,
                random,
                states
            );

            Log.Debug(
                $"{addressesHex} RuneSummon Exec. finished: {DateTimeOffset.UtcNow - started} Elapsed");

            avatarState.blockIndex = context.BlockIndex;
            avatarState.updatedAt = context.BlockIndex;

            // Set states
            return states.SetAvatarState(AvatarAddress, avatarState);
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [AvatarAddressKey] = AvatarAddress.Serialize(),
                [GroupIdKey] = (Integer) GroupId,
                [SummonCountKey] = (Integer) SummonCount,
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue[AvatarAddressKey].ToAddress();
            GroupId = (Integer) plainValue[GroupIdKey];
            SummonCount = (Integer) plainValue[SummonCountKey];
        }

        Address IRuneSummonV1.AvatarAddress => AvatarAddress;
        int IRuneSummonV1.GroupId => GroupId;
        int IRuneSummonV1.SummonCount => SummonCount;

        public static IWorld Summon(
            IActionContext context,
            Address avatarAddress,
            RuneSheet runeSheet,
            SummonSheet.Row summonRow,
            int summonCount,
            IRandom random,
            IWorld states
        )
        {
            var result = SimulateSummon(runeSheet, summonRow, summonCount, random, runeListSheet: null);
#pragma warning disable LAA1002
            foreach (var pair in result)
#pragma warning restore LAA1002
            {
                states = states.MintAsset(context, avatarAddress, pair.Key * pair.Value);
            }

            return states;
        }

        /// <summary>
        /// Simulates rune summoning with optional grade guarantee system.
        /// Applies 10+1 bonus rule and uses grade guarantee settings from SummonSheet.Row when enabled.
        /// When grade guarantee is enabled, ensures minimum grade runes are obtained based on summon count.
        /// Each summon yields 10 runes of the selected type.
        /// </summary>
        /// <param name="runeSheet">Rune sheet containing rune information</param>
        /// <param name="summonRow">Summon configuration row with recipes and guarantee settings</param>
        /// <param name="summonCount">Number of summons to perform (before 10+1 bonus)</param>
        /// <param name="random">Random number generator</param>
        /// <param name="runeListSheet">Rune list sheet for grade information</param>
        /// <returns>Dictionary mapping rune currencies to quantities</returns>
        public static Dictionary<Currency, int> SimulateSummon(
            RuneSheet runeSheet,
            SummonSheet.Row summonRow,
            int summonCount,
            IRandom random,
            RuneListSheet runeListSheet = null
        )
        {
            summonCount = SummonHelper.CalculateSummonCount(summonCount);

            var result = new Dictionary<Currency, int>();
            List<int> recipeIds;
            var useGradeGuarantee = summonRow.UseGradeGuarantee(summonCount);

            if (useGradeGuarantee)
            {
                // For runes, we'll use a simplified approach since runes don't have traditional grades
                // We'll use the same logic but with rune-specific grade checking
                recipeIds = GetRuneSummonRecipeIdsWithGradeGuarantee(
                    summonRow, summonCount, random, runeSheet, runeListSheet);
            }
            else
            {
                // Use original random selection - don't pre-generate, process one by one
                recipeIds = null; // Will be processed one by one in the loop
            }

            if (useGradeGuarantee && recipeIds != null)
            {
                // Process pre-generated recipe IDs
                foreach (var recipeId in recipeIds)
                {
                    // Validate RecipeId
                    var runeRow = runeSheet.OrderedList.FirstOrDefault(r => r.Id == recipeId);
                    if (runeRow is null)
                    {
                        throw new SheetRowNotFoundException(
                            nameof(RuneSheet),
                            recipeId
                        );
                    }

                    var ticker = runeRow.Ticker;
                    var currency = Currencies.GetRune(ticker);
                    result.TryAdd(currency, 0);
                    result[currency] += RuneQuantity;
                }
            }
            else
            {
                // Original logic - process one by one
                for (var i = 0; i < summonCount; i++)
                {
                    var recipeId = SummonHelper.GetSummonRecipeIdByRandom(summonRow, random);

                    // Validate RecipeId
                    var runeRow = runeSheet.OrderedList.FirstOrDefault(r => r.Id == recipeId);
                    if (runeRow is null)
                    {
                        throw new SheetRowNotFoundException(
                            nameof(RuneSheet),
                            recipeId
                        );
                    }

                    var ticker = runeRow.Ticker;
                    var currency = Currencies.GetRune(ticker);
                    result.TryAdd(currency, 0);
                    result[currency] += RuneQuantity;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets rune summon recipe IDs with grade guarantee system based on summon count.
        /// Applies different guarantee settings for 11 summons vs 110 summons.
        /// For runes, we use RuneListSheet.Grade to determine rune quality levels.
        /// This method processes items one by one to maintain the same random call order as the original implementation.
        /// Uses grade guarantee settings from SummonSheet.Row to ensure minimum grade runes are obtained.
        /// </summary>
        /// <param name="summonRow">Summon configuration row with recipes and guarantee settings</param>
        /// <param name="summonCount">Number of summons to perform</param>
        /// <param name="random">Random number generator</param>
        /// <param name="runeSheet">Rune sheet containing rune information</param>
        /// <param name="runeListSheet">Rune list sheet for grade information</param>
        /// <returns>List of recipe IDs with grade guarantee applied</returns>
        private static List<int> GetRuneSummonRecipeIdsWithGradeGuarantee(
            SummonSheet.Row summonRow,
            int summonCount,
            IRandom random,
            RuneSheet runeSheet,
            RuneListSheet runeListSheet)
        {
            var result = new List<int>();
            var guaranteedCount = 0;

            // Get guarantee settings
            var (useGuarantee, minimumGrade, guaranteeCount) = SummonHelper.GetGuaranteeSettings(summonRow, summonCount);

            // Process each item one by one to maintain random call order
            for (var i = 0; i < summonCount; i++)
            {
                int recipeId;

                // Check if we need grade guarantee
                var needsGuarantee = useGuarantee &&
                                   guaranteedCount < guaranteeCount &&
                                   (summonCount - i) <= (guaranteeCount - guaranteedCount);

                if (needsGuarantee)
                {
                    // Select item from minimum grade or higher
                    recipeId = GetGuaranteedRuneGradeRecipeId(summonRow, random, runeSheet, minimumGrade, runeListSheet);
                    guaranteedCount++;
                }
                else
                {
                    // Select item normally
                    recipeId = SummonHelper.GetSummonRecipeIdByRandom(summonRow, random);
                }

                result.Add(recipeId);
            }

            return result;
        }


        /// <summary>
        /// Gets a rune recipe ID that guarantees minimum grade or higher.
        /// Filters available recipes to only those meeting the minimum grade requirement,
        /// then selects from eligible recipes based on their configured ratios.
        /// </summary>
        /// <param name="summonRow">Summon configuration row with recipes</param>
        /// <param name="random">Random number generator</param>
        /// <param name="runeSheet">Rune sheet containing rune information</param>
        /// <param name="minimumGrade">Minimum grade requirement</param>
        /// <param name="runeListSheet">Rune list sheet for grade information</param>
        /// <returns>Recipe ID that meets minimum grade requirement</returns>
        private static int GetGuaranteedRuneGradeRecipeId(
            SummonSheet.Row summonRow,
            IRandom random,
            RuneSheet runeSheet,
            int minimumGrade,
            RuneListSheet runeListSheet)
        {
            // Filter recipes that meet minimum grade requirement
            var eligibleRecipes = new List<(int recipeId, int ratio)>();
            var totalEligibleRatio = 0;

            foreach (var (recipeId, ratio) in summonRow.Recipes)
            {
                var runeRow = runeSheet.OrderedList.FirstOrDefault(r => r.Id == recipeId);
                if (runeRow != null && runeListSheet != null)
                {
                    if (runeListSheet.TryGetValue(recipeId, out var runeListRow) && runeListRow.Grade >= minimumGrade)
                    {
                        eligibleRecipes.Add((recipeId, ratio));
                        totalEligibleRatio += ratio;
                    }
                }
            }

            // If no eligible recipes found, return the first recipe (fallback)
            if (eligibleRecipes.Count == 0)
            {
                return summonRow.Recipes.First().Item1;
            }

            // Select from eligible recipes based on their ratios
            var targetRatio = random.Next(1, totalEligibleRatio + 1);
            var cumulativeRatio = 0;

            foreach (var (recipeId, ratio) in eligibleRecipes)
            {
                cumulativeRatio += ratio;
                if (targetRatio <= cumulativeRatio)
                {
                    return recipeId;
                }
            }

            return eligibleRecipes.First().recipeId;
        }
    }
}
