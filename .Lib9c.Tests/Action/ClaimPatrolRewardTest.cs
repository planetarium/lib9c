namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class ClaimPatrolRewardTest
    {
        private readonly IWorld _initialState;
        private readonly TableSheets _tableSheets;

        public ClaimPatrolRewardTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new World(MockUtil.MockModernWorldState);

            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);
        }

        [Fact]
        public void Execute()
        {
            var privateKey = new PrivateKey();
            var agentAddress = privateKey.Address;
            var row = _tableSheets.PatrolRewardSheet.Values.First();
            var (state, avatar, _) = InitializeUtil.AddAvatar(_initialState, _tableSheets.GetAvatarSheets(), agentAddress);
            var avatarAddress = avatar.address;
            var action = new ClaimPatrolReward(avatar.address);
            var blockIndex = row.StartedBlockIndex;

            var nextState = action.Execute(new ActionContext
            {
                Signer = agentAddress,
                BlockIndex = blockIndex,
                PreviousState = state,
                RandomSeed = 0,
            });

            var avatarState = nextState.GetAvatarState(avatarAddress);
            var itemSheet = _tableSheets.ItemSheet;
            var mail = Assert.IsType<PatrolRewardMail>(avatarState.mailBox.Single());

            foreach (var reward in row.Rewards)
            {
                var ticker = reward.Ticker;
                if (string.IsNullOrEmpty(ticker))
                {
                    var itemId = reward.ItemId;
                    var rowId = itemSheet[itemId].Id;
                    Assert.True(avatarState.inventory.HasItem(rowId, reward.Count));
                    var item = mail.Items.First(i => i.id == itemId);
                    Assert.Equal(item.count, reward.Count);
                }
                else
                {
                    var currency = Currencies.GetMinterlessCurrency(ticker);
                    var recipient = Currencies.PickAddress(currency, agentAddress, avatarAddress);
                    var fav = nextState.GetBalance(recipient, currency);
                    Assert.Equal(currency * reward.Count, fav);
                    Assert.Contains(fav, mail.FungibleAssetValues);
                }
            }

            Assert.Equal(blockIndex, nextState.GetPatrolRewardClaimedBlockIndex(avatarAddress));
            Assert.True(row.Interval > 1L);

            // Throw RequiredBlockIndex by reward interval
            Assert.Throws<RequiredBlockIndexException>(() => action.Execute(new ActionContext
            {
                Signer = agentAddress,
                BlockIndex = blockIndex + 1L,
                PreviousState = nextState,
                RandomSeed = 0,
            }));
        }

        [Fact]
        public void Execute_Throw_InvalidAddressException()
        {
            var signer = new PrivateKey().Address;
            var action = new ClaimPatrolReward(signer);

            Assert.Throws<InvalidAddressException>(() => action.Execute(new ActionContext
            {
                Signer = signer,
                BlockIndex = 0,
                PreviousState = _initialState,
            }));
        }
    }
}
