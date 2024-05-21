namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;
    using static Nekoyume.Model.State.RedeemCodeState;

    public class RedeemCodeTest
    {
        private readonly Address _agentAddress = new Address(new byte[]
        {
            0x10, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x02,
        });

        private readonly Address _avatarAddress = new Address(new byte[]
        {
            0x10, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x01,
        });

        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        public RedeemCodeTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);
        }

        [Fact]
        public void Execute()
        {
            var privateKey = new PrivateKey();
            PublicKey publicKey = privateKey.PublicKey;
            var prevRedeemCodesState = new RedeemCodeState(new Dictionary<PublicKey, Reward>()
            {
                [publicKey] = new Reward(1),
            });
            var gameConfigState = new GameConfigState();
            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses[0] = _avatarAddress;
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.GetAvatarSheets(),
                default
            );

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var goldState = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618

            var context = new ActionContext();
            var initialState = new World(MockUtil.MockModernWorldState)
                .SetAgentState(_agentAddress, agentState)
                .SetLegacyState(RedeemCodeState.Address, prevRedeemCodesState.Serialize())
                .SetLegacyState(GoldCurrencyState.Address, goldState.Serialize())
                .MintAsset(context, GoldCurrencyState.Address, goldState.Currency * 100000000)
                .SetAvatarState(_avatarAddress, avatarState);

            foreach (var (key, value) in _sheets)
            {
                initialState = initialState.SetLegacyState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize()
                );
            }

            var redeemCode = new RedeemCode(
                ByteUtil.Hex(privateKey.ByteArray),
                _avatarAddress
            );

            IWorld nextState = redeemCode.Execute(new ActionContext()
            {
                BlockIndex = 1,
                Miner = default,
                PreviousState = initialState,
                Signer = _agentAddress,
                RandomSeed = 0,
            });

            // Check target avatar & agent
            AvatarState nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            // See also Data/TableCSV/RedeemRewardSheet.csv
            ItemSheet itemSheet = initialState.GetItemSheet();
            HashSet<int> expectedItems = new[] { 302006, 302004, 302001, 302002 }.ToHashSet();
            Assert.Subset(nextAvatarState.inventory.Items.Select(i => i.item.Id).ToHashSet(), expectedItems);

            // Check the code redeemed properly
            RedeemCodeState nextRedeemCodeState = nextState.GetRedeemCodeState();
            Assert.Throws<DuplicateRedeemException>(() =>
            {
                nextRedeemCodeState.Redeem(redeemCode.Code, redeemCode.AvatarAddress);
            });
        }
    }
}
