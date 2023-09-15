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
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class UnlockWorldTest
    {
        private readonly IRandom _random;
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;
        private readonly Currency _currency;
        private readonly IWorld _initialWorld;

        public UnlockWorldTest()
        {
            _random = new TestRandom();
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = new PrivateKey().ToAddress();
            _currency = CrystalCalculator.CRYSTAL;
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

            _initialWorld = new MockWorld();
            _initialWorld = LegacyModule.SetState(
                _initialWorld,
                Addresses.GetSheetAddress<WorldUnlockSheet>(),
                _tableSheets.WorldUnlockSheet.Serialize());
            _initialWorld = LegacyModule.SetState(
                _initialWorld,
                Addresses.GameConfig,
                gameConfigState.Serialize());
        }

        [Theory]
        [InlineData(new[] { 2 }, true,  false, true, 500, null)]
        // Migration AvatarState.
        [InlineData(new[] { 2, 3, 4, 5 }, true,  false, true, 153000, null)]
        // TODO: add world 6 unlock
        //[InlineData(new[] { 2, 3, 4, 5, 6 }, true, true, false, true, 153000, null)]
        // Try open Yggdrasil.
        [InlineData(new[] { 1 }, false,  false, true, 0, typeof(InvalidWorldException))]
        // Try open Mimisbrunnr.
        [InlineData(new[] { GameConfig.MimisbrunnrWorldId }, false,  false, true, 0, typeof(InvalidWorldException))]
        // Empty WorldId.
        [InlineData(new int[] { }, false,  false, true, 0, typeof(InvalidWorldException))]
        // AvatarState is null.
        [InlineData(new[] { 2 }, false,  false, true, 0, typeof(FailedLoadStateException))]
        // Already unlocked world.
        [InlineData(new[] { 2 }, true,  true, true, 0, typeof(AlreadyWorldUnlockedException))]
        // Skip previous world.
        [InlineData(new[] { 3 }, true,  false, true, 0, typeof(FailedToUnlockWorldException))]
        // Stage not cleared.
        [InlineData(new[] { 2 }, true,  false, false, 0, typeof(FailedToUnlockWorldException))]
        // Insufficient CRYSTAL.
        [InlineData(new[] { 2 }, true,  false, true, 100, typeof(NotEnoughFungibleAssetValueException))]
        public void Execute(
            IEnumerable<int> ids,
            bool stateExist,
            bool alreadyUnlocked,
            bool stageCleared,
            int balance,
            Type exc
        )
        {
            var context = new ActionContext();
            var state = (balance > 0)
                ? LegacyModule.MintAsset(_initialWorld, context, _agentAddress, balance * _currency)
                : _initialWorld;
            var worldIds = ids.ToList();

            if (stateExist)
            {
                var worldInformation = _avatarState.worldInformation;
                if (stageCleared)
                {
                    foreach (var wordId in worldIds)
                    {
                        var row = _tableSheets.WorldUnlockSheet.OrderedList.First(r =>
                            r.WorldIdToUnlock == wordId);
                        var worldRow = _tableSheets.WorldSheet[row.WorldId];
                        var prevRow =
                            _tableSheets.WorldUnlockSheet.OrderedList.FirstOrDefault(r =>
                                r.WorldIdToUnlock == row.WorldId);
                        // Clear prev world.
                        if (!(prevRow is null))
                        {
                            var prevWorldRow = _tableSheets.WorldSheet[prevRow.WorldId];
                            for (int i = prevWorldRow.StageBegin; i < prevWorldRow.StageEnd + 1; i++)
                            {
                                worldInformation.ClearStage(prevWorldRow.Id, i, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
                            }
                        }

                        for (int i = worldRow.StageBegin; i < worldRow.StageEnd + 1; i++)
                        {
                            worldInformation.ClearStage(worldRow.Id, i, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
                        }
                    }
                }

                state = AvatarModule.SetAvatarState(
                    state,
                    _avatarAddress,
                    _avatarState,
                    true,
                    true,
                    true,
                    true);
            }

            var unlockedWorldIdsAddress = _avatarAddress.Derive("world_ids");
            if (alreadyUnlocked)
            {
                var unlockIds = List.Empty.Add(1.Serialize());
                foreach (var worldId in worldIds)
                {
                    unlockIds = unlockIds.Add(worldId.Serialize());
                }

                state = LegacyModule.SetState(state, unlockedWorldIdsAddress, unlockIds);
            }

            var action = new UnlockWorld
            {
                WorldIds = worldIds,
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

                var nextAccount = nextWorld.GetAccount(ReservedAddresses.LegacyAccount);

                Assert.True(LegacyModule.TryGetState(nextWorld, unlockedWorldIdsAddress, out List rawIds));

                var unlockedIds = rawIds.ToList(StateExtensions.ToInteger);

                Assert.All(worldIds, worldId => Assert.Contains(worldId, unlockedIds));
                Assert.Equal(0 * _currency, nextAccount.GetBalance(_agentAddress, _currency));
                Assert.Equal(balance * _currency, nextAccount.GetBalance(Addresses.UnlockWorld, _currency));
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
    }
}
