using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Pet;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1158
    /// Updated at https://github.com/planetarium/lib9c/pull/1158
    /// </summary>
    [Serializable]
    [ActionType("create_avatar8")]
    public class CreateAvatar : GameAction, ICreateAvatarV2
    {
        public const string DeriveFormat = "avatar-state-{0}";

        public int index;
        public int hair;
        public int lens;
        public int ear;
        public int tail;
        public string name;

        int ICreateAvatarV2.Index => index;
        int ICreateAvatarV2.Hair => hair;
        int ICreateAvatarV2.Lens => lens;
        int ICreateAvatarV2.Ear => ear;
        int ICreateAvatarV2.Tail => tail;
        string ICreateAvatarV2.Name => name;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>()
        {
            ["index"] = (Integer) index,
            ["hair"] = (Integer) hair,
            ["lens"] = (Integer) lens,
            ["ear"] = (Integer) ear,
            ["tail"] = (Integer) tail,
            ["name"] = (Text) name,
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            index = (int) ((Integer) plainValue["index"]).Value;
            hair = (int) ((Integer) plainValue["hair"]).Value;
            lens = (int) ((Integer) plainValue["lens"]).Value;
            ear = (int) ((Integer) plainValue["ear"]).Value;
            tail = (int) ((Integer) plainValue["tail"]).Value;
            name = (Text) plainValue["name"];
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            var avatarAddress = ctx.Signer.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DeriveFormat,
                    index
                )
            );
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            if (ctx.Rehearsal)
            {
                states = states.SetState(ctx.Signer, MarkChanged);
                for (var i = 0; i < AvatarState.CombinationSlotCapacity; i++)
                {
                    var slotAddress = avatarAddress.Derive(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            CombinationSlotState.DeriveFormat,
                            i
                        )
                    );
                    states = states.SetState(slotAddress, MarkChanged);
                }

                return states
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, ctx.Signer);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            if (!Regex.IsMatch(name, GameConfig.AvatarNickNamePattern))
            {
                throw new InvalidNamePatternException(
                    $"{addressesHex}Aborted as the input name {name} does not follow the allowed name pattern.");
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}CreateAvatar exec started", addressesHex);
            AgentState existingAgentState = states.GetAgentState(ctx.Signer);
            var agentState = existingAgentState ?? new AgentState(ctx.Signer);
            var avatarState = states.GetAvatarState(avatarAddress);
            if (!(avatarState is null))
            {
                throw new InvalidAddressException(
                    $"{addressesHex}Aborted as there is already an avatar at {avatarAddress}.");
            }

            if (!(0 <= index && index < GameConfig.SlotCount))
            {
                throw new AvatarIndexOutOfRangeException(
                    $"{addressesHex}Aborted as the index is out of range #{index}.");
            }

            if (agentState.avatarAddresses.ContainsKey(index))
            {
                throw new AvatarIndexAlreadyUsedException(
                    $"{addressesHex}Aborted as the signer already has an avatar at index #{index}.");
            }
            sw.Stop();
            Log.Verbose("{AddressesHex}CreateAvatar Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            Log.Verbose("{AddressesHex}Execute CreateAvatar; player: {AvatarAddress}", addressesHex, avatarAddress);

            agentState.avatarAddresses.Add(index, avatarAddress);

            // Avoid NullReferenceException in test
            var materialItemSheet = ctx.PreviousStates.GetSheet<MaterialItemSheet>();

            avatarState = CreateAvatar0.CreateAvatarState(name, avatarAddress, ctx, materialItemSheet, default);

            if (hair < 0) hair = 0;
            if (lens < 0) lens = 0;
            if (ear < 0) ear = 0;
            if (tail < 0) tail = 0;

            avatarState.Customize(hair, lens, ear, tail);

            foreach (var address in avatarState.combinationSlotAddresses)
            {
                var slotState =
                    new CombinationSlotState(address, GameConfig.RequireClearedStageLevel.CombinationEquipmentAction);
                states = states.SetState(address, slotState.Serialize());
            }

            avatarState.UpdateQuestRewards(materialItemSheet);

            // Add Runes when executing on editor mode.
#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            states = CreateAvatar0.AddRunesForTest(avatarAddress, states);

            // Add pets for test
            if (states.TryGetSheet(out PetSheet petSheet))
            {
                foreach (var row in petSheet)
                {
                    var petState = new PetState(row.Id);
                    petState.LevelUp();
                    var petStateAddress = PetState.DeriveAddress(avatarAddress, row.Id);
                    states = states.SetState(petStateAddress, petState.Serialize());
                }
            }
#endif
            // Prepare previewnet test
            // Clear stage.
            for (int i = 0; i < 250; i++)
            {
                avatarState.worldInformation.ClearStage(
                    worldId: (i / 50) + 1,
                    stageId: i + 1,
                    clearedAt: 0,
                    ctx.PreviousStates.GetSheet<WorldSheet>(),
                    ctx.PreviousStates.GetSheet<WorldUnlockSheet>());
            }

            var recipeIds = new int[] {
                21,     // 10140000, war sword
                62,     // 10240000, war armor
                103,    // 10350000, legendary belt
                128,    // 10450000, legendary necklace
                148,    // 10540000, warrior's ring
                152,    // 10544000, warrior's ring (wind)
            };
            var equipmentSheet = states.GetSheet<EquipmentItemSheet>();
            var recipeSheet = states.GetSheet<EquipmentItemRecipeSheet>();
            var subRecipeSheet = states.GetSheet<EquipmentItemSubRecipeSheetV2>();
            var optionSheet = states.GetSheet<EquipmentItemOptionSheet>();
            var skillSheet = states.GetSheet<SkillSheet>();
            var characterLevelSheet = states.GetSheet<CharacterLevelSheet>();
            var enhancementCostSheet = states.GetSheet<EnhancementCostSheetV2>();
            var costumeSheet = states.GetSheet<CostumeItemSheet>();

            avatarState.level = 290;
            avatarState.exp = characterLevelSheet[290].Exp;

            // prepare equipments for test
            foreach (var recipeId in recipeIds)
            {
                var recipeRow = recipeSheet[recipeId];
                var subRecipeId = recipeRow.SubRecipeIds[1];
                var subRecipeRow = subRecipeSheet[subRecipeId];
                var equipmentRow = equipmentSheet[recipeRow.ResultEquipmentId];

                for (int i = 0; i < 10; i++)
                {
                    var equipment = (Equipment)ItemFactory.CreateItemUsable(
                        equipmentRow,
                        context.Random.GenerateRandomGuid(),
                        0L,
                        madeWithMimisbrunnrRecipe: recipeRow.IsMimisBrunnrSubRecipe(subRecipeId));

                    foreach (var option in subRecipeRow.Options)
                    {
                        var optionRow = optionSheet[option.Id];
                        // Add stats.
                        if (optionRow.StatType != StatType.NONE)
                        {
                            var statMap = new StatMap(optionRow.StatType, optionRow.StatMax);
                            equipment.StatsMap.AddStatAdditionalValue(statMap.StatType, statMap.Value);
                            equipment.optionCountFromCombination++;
                        }
                        // Add skills.
                        else
                        {
                            var skillRow = skillSheet.OrderedList.First(r => r.Id == optionRow.SkillId);
                            var skill = SkillFactory.Get(skillRow, optionRow.SkillDamageMax, optionRow.SkillChanceMax);
                            if (skill != null)
                            {
                                equipment.Skills.Add(skill);
                                equipment.optionCountFromCombination++;
                            }
                        }
                    }

                    avatarState.inventory.AddItem(equipment);
                }
            }

            // prepare materials
            var materialMap = new (int materialId, int count)[]
            {
                // hourglass
                (400000, 100_000_000),
                // ap potion
                (500000, 100)
            };
            foreach (var (materialId, count) in materialMap)
            {
                var tradableMaterial =
                    ItemFactory.CreateTradableMaterial(materialItemSheet[materialId]);
                avatarState.inventory.AddItem(tradableMaterial, count);
            }

            // prepare costume
            var costumeIds = new[]
            {
                // Lily the Wizard
                40100008,
                // The Black Wizard Evely
                40100009,
                // The Axe Warrior Furyosa
                40100010
            };

            foreach (var costumeId in costumeIds)
            {
                var costume = ItemFactory.CreateCostume(costumeSheet[costumeId],
                    context.Random.GenerateRandomGuid());
                avatarState.inventory.AddItem(costume);
            }
            // mint assets.
            var ncgCurrency = states.GetGoldCurrency();
            states = states.MintAsset(ctx.Signer, 50_000 * ncgCurrency);
            var soulStones = new List<(int id, int count)>
            {
                // Soulstone_1001
                (1001, 2_500),
                // Soulstone_1002
                (1002, 2_500),
                // Solustone_1003
                (1003, 8_000),
                // Solustone_1004
                (1004, 8_000)
            };
            var pet = states.GetSheet<PetSheet>();
            foreach (var values in soulStones)
            {
                var row = pet[values.id];
                var soulStoneCurrency = Currency.Legacy(
                    row.SoulStoneTicker,
                    0,
                    minters: null);
                var soulStone = values.count * soulStoneCurrency;
                states = states.MintAsset(avatarAddress, soulStone);
            }

            sw.Stop();
            Log.Verbose("{AddressesHex}CreateAvatar CreateAvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}CreateAvatar Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetState(ctx.Signer, agentState.Serialize())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2())
                .MintAsset(ctx.Signer, 1_000_000_000 * CrystalCalculator.CRYSTAL);
        }
    }
}
