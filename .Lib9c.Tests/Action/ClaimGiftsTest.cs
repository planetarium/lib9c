namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Lib9c.Action;
    using Lib9c.Model.Item;
    using Lib9c.Model.State;
    using Lib9c.Module;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
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

        [Fact]
        public void Execute_Success()
        {
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = Addresses.GetAvatarAddress(agentAddress, 0);

            var avatarState = AvatarState.Create(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                default);
            var state = _state.SetAvatarState(avatarAddress, avatarState);
            var row = _tableSheets.ClaimableGiftsSheet.Values.FirstOrDefault(r => r.StartedBlockIndex > 0L);
            if (row is not null)
            {
                Execute(
                    state,
                    avatarAddress,
                    agentAddress,
                    row.Id,
                    row.StartedBlockIndex,
                    row.Items.ToArray()
                );
            }
        }

        [Fact]
        public void Execute_ClaimableGiftsNotAvailableException()
        {
            var agentAddress = new PrivateKey().Address;
            var avatarAddress = Addresses.GetAvatarAddress(agentAddress, 0);

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
            var avatarAddress = Addresses.GetAvatarAddress(agentAddress, 0);

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
            (int itemId, int quantity, bool tradable)[] expected)
        {
            var prevClaimedGifts = _state.GetClaimedGifts(avatarAddress);

            var action = new ClaimGifts(avatarAddress, giftId);
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
            foreach (var (itemId, quantity, tradable) in expected)
            {
                Assert.True(inventory.TryGetItem(itemId, out var inventoryItem));
                Assert.Equal(quantity, inventoryItem.count);
                Assert.Equal(tradable, inventoryItem.item is ITradableItem);
            }

            return nextState;
        }
    }
}
