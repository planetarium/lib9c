namespace Lib9c.Tests.Action
{
    using System;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class ClaimGiftsTest
    {
        private readonly TableSheets _tableSheets;
        private readonly IWorld _state;

        public ClaimGiftsTest()
        {
            _state = new World(MockUtil.MockModernWorldState);

            var tableCsv = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in tableCsv)
            {
                _state = _state.SetLegacyState(Addresses.GetSheetAddress(key), value.Serialize());
            }

            _tableSheets = new TableSheets(tableCsv);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(300)]
        [InlineData(600)]
        [InlineData(1200)]
        public void Execute_Success(long blockIndex)
        {
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;

            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default);
            var state = _state.SetAvatarState(avatarAddress, avatarState);

            if (!_tableSheets.ClaimableGiftsSheet.TryFindRowByBlockIndex(blockIndex, out var row))
            {
                throw new Exception();
            }

            Execute(
                state,
                avatarAddress,
                agentAddress,
                row.Id,
                blockIndex,
                row.Items.ToArray()
            );
        }

        [Fact]
        public void Execute_ClaimableGiftsNotAvailableException()
        {
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;

            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default);
            var state = _state.SetAvatarState(avatarAddress, avatarState);
            var sheet = _tableSheets.ClaimableGiftsSheet;

            Assert.Throws<ClaimableGiftsNotAvailableException>(() =>
            {
                var row = sheet.Values.OrderBy(row => row.StartedBlockIndex).First();
                Execute(
                    state,
                    avatarAddress,
                    agentAddress,
                    row.Id,
                    row.StartedBlockIndex - 1,
                    row.Items.ToArray()
                );
            });
            Assert.Throws<ClaimableGiftsNotAvailableException>(() =>
            {
                var row = sheet.Values.OrderByDescending(row => row.EndedBlockIndex).First();
                Execute(
                    state,
                    avatarAddress,
                    agentAddress,
                    row.Id,
                    row.EndedBlockIndex + 1,
                    row.Items.ToArray()
                );
            });
        }

        [Fact]
        public void Execute_AlreadyClaimedGiftsException()
        {
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = new PrivateKey().Address;

            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default);
            var state = _state.SetAvatarState(avatarAddress, avatarState);

            var row = _tableSheets.ClaimableGiftsSheet.Values.First();
            var blockIndex = row.StartedBlockIndex;

            var nextState = Execute(
                state,
                avatarAddress,
                agentAddress,
                row.Id,
                blockIndex,
                row.Items.ToArray()
            );
            Assert.Throws<AlreadyClaimedGiftsException>(() =>
            {
                Execute(
                    nextState,
                    avatarAddress,
                    agentAddress,
                    row.Id,
                    blockIndex + 1,
                    row.Items.ToArray()
                );
            });
        }

        private IWorld Execute(
            IWorld previousState,
            Address avatarAddress,
            Address agentAddress,
            int giftId,
            long blockIndex,
            (int itemId, int quantity)[] expected)
        {
            var prevClaimedGifts = _state.GetClaimedGifts(avatarAddress);

            var action = new ClaimGifts
            {
                AvatarAddress = avatarAddress,
                GiftId = giftId,
            };
            var actionContext = new ActionContext
            {
                PreviousState = previousState,
                Signer = agentAddress,
                BlockIndex = blockIndex,
            };

            var nextState = action.Execute(actionContext);

            // Check claimed gifts.
            var nextClaimedGifts = nextState.GetClaimedGifts(avatarAddress);
            Assert.Equal(prevClaimedGifts.Count + 1, nextClaimedGifts.Count);

            // Check Inventory.
            var inventory = nextState.GetInventoryV2(avatarAddress);
            foreach (var (itemId, quantity) in expected)
            {
                Assert.True(inventory.TryGetItem(itemId, out var inventoryItem));
                Assert.Equal(quantity, inventoryItem.count);
            }

            return nextState;
        }
    }
}
