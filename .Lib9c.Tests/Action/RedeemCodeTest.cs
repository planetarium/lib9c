namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Security.Cryptography;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
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
        private readonly PrivateKey _privateKey;
        private readonly GoldCurrencyState _goldState;
        private readonly IAccountStateDelta _initialState;

        public RedeemCodeTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);

            _privateKey = new PrivateKey();
            PublicKey publicKey = _privateKey.PublicKey;
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

            _goldState = new GoldCurrencyState(new Currency("NCG", 2, minter: null));

            _initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(RedeemCodeState.Address, prevRedeemCodesState.Serialize())
                .SetState(GoldCurrencyState.Address, _goldState.Serialize())
                .MintAsset(GoldCurrencyState.Address, _goldState.Currency * 100000000);

            foreach (var (key, value) in _sheets)
            {
                _initialState = _initialState.SetState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize()
                );
            }
        }

        [Fact]
        public void Execute()
        {
            var redeemCode = new RedeemCode(
                ByteUtil.Hex(_privateKey.ByteArray),
                _avatarAddress
            );

            IAccountStateDelta nextState = redeemCode.Execute(new ActionContext()
            {
                BlockIndex = 1,
                Miner = default,
                PreviousStates = _initialState,
                Rehearsal = false,
                Signer = _agentAddress,
            });

            // Check target avatar & agent
            AvatarState nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            // See also Data/TableCSV/RedeemRewardSheet.csv
            ItemSheet itemSheet = _initialState.GetItemSheet();
            HashSet<int> expectedItems = new[] { 100000, 40100000 }.ToHashSet();
            Assert.Subset(nextAvatarState.inventory.Items.Select(i => i.item.Id).ToHashSet(), expectedItems);
            Assert.Equal(_goldState.Currency * 100, nextState.GetBalance(_agentAddress, _goldState.Currency));

            // Check the code redeemed properly
            RedeemCodeState nextRedeemCodeState = nextState.GetRedeemCodeState();
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

            IAccountStateDelta nextState = redeemCode.Execute(new ActionContext()
            {
                BlockIndex = 1,
                Miner = default,
                PreviousStates = new State(),
                Rehearsal = true,
                Signer = _agentAddress,
            });

            Assert.Equal(
                nextState.UpdatedAddresses,
                new[] { _avatarAddress, _agentAddress, RedeemCodeState.Address, GoldCurrencyState.Address }.ToImmutableHashSet()
            );
        }

        [Fact]
        public void Determinism()
        {
            var action = new RedeemCode(
                ByteUtil.Hex(_privateKey.ByteArray),
                _avatarAddress
            );

            HashDigest<SHA256> stateRootHashA = ActionExecutionUtils.CalculateStateRootHash(action, previousStates: _initialState, signer: _agentAddress);
            HashDigest<SHA256> stateRootHashB = ActionExecutionUtils.CalculateStateRootHash(action, previousStates: _initialState, signer: _agentAddress);

            Assert.Equal(stateRootHashA, stateRootHashB);
        }
    }
}
