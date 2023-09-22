namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;
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
                gameConfigState,
                default
            );

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var goldState = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618

            var context = new ActionContext();
            var initialState = AgentModule.SetAgentState(
                new MockWorld(),
                _agentAddress,
                agentState);
            initialState = LegacyModule.SetState(
                initialState,
                RedeemCodeState.Address,
                prevRedeemCodesState.Serialize());
            initialState = LegacyModule.SetState(
                initialState,
                GoldCurrencyState.Address,
                goldState.Serialize());
            initialState = LegacyModule.MintAsset(
                initialState,
                context,
                GoldCurrencyState.Address,
                goldState.Currency * 100000000);

            initialState = AvatarModule.SetAvatarState(
                initialState,
                _avatarAddress,
                avatarState,
                true,
                true,
                true,
                true);

            foreach (var (key, value) in _sheets)
            {
                initialState = LegacyModule.SetState(
                    initialState,
                    Addresses.TableSheet.Derive(key),
                    value.Serialize()
                );
            }

            var redeemCode = new RedeemCode(
                ByteUtil.Hex(privateKey.ByteArray),
                _avatarAddress
            );

            var nextWorld = redeemCode.Execute(new ActionContext()
            {
                BlockIndex = 1,
                Miner = default,
                PreviousState = initialState,
                Rehearsal = false,
                Signer = _agentAddress,
                RandomSeed = 0,
            });

            // Check target avatar & agent
            AvatarState nextAvatarState = AvatarModule.GetAvatarState(nextWorld, _avatarAddress);
            // See also Data/TableCSV/RedeemRewardSheet.csv
            ItemSheet itemSheet = LegacyModule.GetItemSheet(nextWorld);
            HashSet<int> expectedItems = new[] { 302006, 302004, 302001, 302002 }.ToHashSet();
            Assert.Subset(nextAvatarState.inventory.Items.Select(i => i.item.Id).ToHashSet(), expectedItems);

            // Check the code redeemed properly
            RedeemCodeState nextRedeemCodeState = LegacyModule.GetRedeemCodeState(nextWorld);
            Assert.Throws<DuplicateRedeemException>(() =>
            {
                nextRedeemCodeState.Redeem(redeemCode.Code, redeemCode.AvatarAddress);
            });
        }

        [Fact]
        public void Rehearsal()
        {
            var redeemCode = new RedeemCode(
                string.Empty,
                _avatarAddress
            );

            IWorld nextState = redeemCode.Execute(new ActionContext()
            {
                BlockIndex = 1,
                Miner = default,
                PreviousState = new MockWorld(),
                Rehearsal = true,
                Signer = _agentAddress,
            });

            Assert.Equal(
                new[]
                {
                    _agentAddress,
                    RedeemCodeState.Address,
                    GoldCurrencyState.Address,
                }.ToImmutableHashSet(),
                nextState.GetAccount(ReservedAddresses.LegacyAccount).Delta.UpdatedAddresses
            );

            Assert.Equal(
                new[] { _avatarAddress }.ToImmutableHashSet(),
                nextState.GetAccount(Addresses.Avatar).Delta.UpdatedAddresses);
            Assert.Equal(
                new[] { _avatarAddress }.ToImmutableHashSet(),
                nextState.GetAccount(Addresses.Inventory).Delta.UpdatedAddresses);
            Assert.Equal(
                new[] { _avatarAddress }.ToImmutableHashSet(),
                nextState.GetAccount(Addresses.WorldInformation).Delta.UpdatedAddresses);
            Assert.Equal(
                new[] { _avatarAddress }.ToImmutableHashSet(),
                nextState.GetAccount(Addresses.QuestList).Delta.UpdatedAddresses);
        }
    }
}
