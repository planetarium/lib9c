namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class UnlockEquipmentRecipeTest
    {
        private readonly IRandom _random;
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;
        private readonly Currency _currency;
        private readonly IWorld _initialState;

        public UnlockEquipmentRecipeTest()
        {
            _random = new TestRandom();
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = new PrivateKey().ToAddress();
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("CRYSTAL", 18, null);
#pragma warning restore CS0618
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);

            var agentState = new AgentState(_agentAddress);
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            );

            agentState.avatarAddresses.Add(0, _avatarAddress);

            _initialState = new MockWorld();
            _initialState = AgentModule.SetAgentState(_initialState, _agentAddress, agentState);
            _initialState = LegacyModule.SetState(_initialState, Addresses.GetSheetAddress<EquipmentItemSheet>(), _tableSheets.EquipmentItemSheet.Serialize());
            _initialState = LegacyModule.SetState(_initialState, Addresses.GetSheetAddress<EquipmentItemRecipeSheet>(), _tableSheets.EquipmentItemRecipeSheet.Serialize());
            _initialState = LegacyModule.SetState(_initialState, Addresses.GameConfig, gameConfigState.Serialize());
        }

        [Theory]
        [InlineData(new[] { 2, 3 }, true, false, false, true, 4, null)]
        [InlineData(new[] { 2 }, true, false, false, true, 2, null)]
        // Unlock Belt without Armor unlock.
        [InlineData(new[] { 83 }, true, false, false, true, 1, null)]
        // Unlock Weapon & Ring
        [InlineData(new[] { 2, 133 }, true, false, false, true, 3, null)]
        // AvatarState migration.
        [InlineData(new[] { 2 }, true, true, false, true, 2, null)]
        // Invalid recipe id.
        [InlineData(new[] { -1 }, true, false, false, false, 100, typeof(InvalidRecipeIdException))]
        [InlineData(new[] { 1 }, true, false, false, true, 100, typeof(InvalidRecipeIdException))]
        [InlineData(new int[] { }, true, false, false, false, 100, typeof(InvalidRecipeIdException))]
        // AvatarState is null.
        [InlineData(new[] { 2 }, false, true, false, true, 100, typeof(FailedLoadStateException))]
        [InlineData(new[] { 2 }, false, false, false, true, 100, typeof(FailedLoadStateException))]
        // Already unlocked recipe.
        [InlineData(new[] { 2 }, true, false, true, true, 100, typeof(AlreadyRecipeUnlockedException))]
        // Skip prev recipe.
        [InlineData(new[] { 3 }, true, false, false, true, 100, typeof(InvalidRecipeIdException))]
        // Stage not cleared.
        [InlineData(new[] { 2 }, true, false, false, false, 100, typeof(NotEnoughClearedStageLevelException))]
        // Insufficient CRYSTAL.
        [InlineData(new[] { 2 }, true, false, false, true, 1, typeof(NotEnoughFungibleAssetValueException))]
        public void Execute(
            IEnumerable<int> ids,
            bool stateExist,
            bool migrationRequired,
            bool alreadyUnlocked,
            bool stageCleared,
            int balance,
            Type exc
        )
        {
            var context = new ActionContext();
            var state = LegacyModule.MintAsset(_initialState, context, _agentAddress, balance * _currency);
            List<int> recipeIds = ids.ToList();
            Address unlockedRecipeIdsAddress = _avatarAddress.Derive("recipe_ids");
            if (stateExist)
            {
                var worldInformation = _avatarState.worldInformation;
                if (stageCleared)
                {
                    var stage = _tableSheets.EquipmentItemRecipeSheet[recipeIds.Max()].UnlockStage;
                    for (int i = 1; i < stage + 1; i++)
                    {
                        worldInformation.ClearStage(1, i, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
                    }
                }
                else
                {
                    Assert.All(recipeIds, recipeId => worldInformation.IsStageCleared(recipeId));
                }

                if (alreadyUnlocked)
                {
                    var serializedIds = new List(recipeIds.Select(i => i.Serialize()));
                    state = LegacyModule.SetState(state, unlockedRecipeIdsAddress, serializedIds);
                }

                if (migrationRequired)
                {
                    state = AvatarModule.SetAvatarState(state, _avatarAddress, _avatarState);
                }
                else
                {
                    state = LegacyModule.SetState(state, _avatarAddress.Derive(LegacyInventoryKey), _avatarState.inventory.Serialize());
                    state = LegacyModule.SetState(state, _avatarAddress.Derive(LegacyWorldInformationKey), worldInformation.Serialize());
                    state = LegacyModule.SetState(state, _avatarAddress.Derive(LegacyQuestListKey), _avatarState.questList.Serialize());
                    state = AvatarModule.SetAvatarStateV2(state, _avatarAddress, _avatarState);
                }
            }

            var action = new UnlockEquipmentRecipe
            {
                RecipeIds = recipeIds.ToList(),
                AvatarAddress = _avatarAddress,
            };

            if (exc is null)
            {
                IWorld nextWorld = action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    Random = _random,
                });
                IAccount nextAccount = nextWorld.GetAccount(ReservedAddresses.LegacyAccount);

                Assert.True(LegacyModule.TryGetState(nextWorld, unlockedRecipeIdsAddress, out List rawIds));

                var unlockedIds = rawIds.ToList(StateExtensions.ToInteger);

                Assert.All(recipeIds, recipeId => Assert.Contains(recipeId, unlockedIds));
                Assert.Equal(0 * _currency, nextAccount.GetBalance(_agentAddress, _currency));
                Assert.Equal(balance * _currency, nextAccount.GetBalance(Addresses.UnlockEquipmentRecipe, _currency));
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    Random = _random,
                }));
            }
        }

        [Theory]
        [InlineData(ItemSubType.Weapon)]
        [InlineData(ItemSubType.Armor)]
        [InlineData(ItemSubType.Belt)]
        [InlineData(ItemSubType.Necklace)]
        [InlineData(ItemSubType.Ring)]
        public void UnlockedIds(ItemSubType itemSubType)
        {
            var worldInformation = _avatarState.worldInformation;
            var rows = _tableSheets.EquipmentItemRecipeSheet.Values
                .Where(i => i.ItemSubType == itemSubType && i.Id != 1 && i.UnlockStage != 999 && i.CRYSTAL != 0);

            // Clear Stage
            for (int i = 0; i < _tableSheets.WorldSheet.Count; i++)
            {
                var worldRow = _tableSheets.WorldSheet.OrderedList[i];
                for (int v = worldRow.StageBegin; v < worldRow.StageEnd + 1; v++)
                {
                    worldInformation.ClearStage(worldRow.Id, v, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
                }
            }

            // Unlock All recipe by ItemSubType
            UnlockEquipmentRecipe.UnlockedIds(_initialState, new PrivateKey().ToAddress(), _tableSheets.EquipmentItemRecipeSheet, worldInformation, rows.Select(i => i.Id).ToList());
        }
    }
}
