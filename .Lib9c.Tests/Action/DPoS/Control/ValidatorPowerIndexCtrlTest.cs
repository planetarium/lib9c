namespace Lib9c.Tests.Action.DPoS.Control
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action.DPoS.Control;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Action.DPoS.Model;
    using Nekoyume.Module;
    using Xunit;

    public class ValidatorPowerIndexCtrlTest : PoSTest
    {
        private readonly ImmutableHashSet<Currency> _nativeTokens;
        private IWorld _states;

        public ValidatorPowerIndexCtrlTest()
        {
            List<PublicKey> operatorPublicKeys = new List<PublicKey>()
            {
                new PrivateKey().PublicKey,
                new PrivateKey().PublicKey,
                new PrivateKey().PublicKey,
                new PrivateKey().PublicKey,
                new PrivateKey().PublicKey,
            };

            List<Address> operatorAddresses = operatorPublicKeys.Select(
                pubKey => pubKey.Address).ToList();

            _nativeTokens = ImmutableHashSet.Create(
                Asset.GovernanceToken, Asset.ConsensusToken, Asset.Share);

            _states = InitializeStates();
            ValidatorAddresses = new List<Address>();

            var pairs = operatorAddresses.Zip(operatorPublicKeys, (addr, key) => (addr, key));
            foreach (var (addr, key) in pairs)
            {
                _states = _states.MintAsset(
                    new ActionContext
                    {
                        PreviousState = _states,
                        BlockIndex = 1,
                    },
                    addr,
                    Asset.GovernanceToken * 100);
                _states = ValidatorCtrl.Create(
                    _states,
                    new ActionContext
                    {
                        PreviousState = _states,
                        BlockIndex = 1,
                    },
                    addr,
                    key,
                    Asset.GovernanceToken * 10,
                    _nativeTokens);
                ValidatorAddresses.Add(Validator.DeriveAddress(addr));
            }
        }

        private List<Address> ValidatorAddresses { get; set; }

        [Fact]
        public void SortingTestDifferentToken()
        {
            _states = _states.MintAsset(
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                ValidatorAddresses[0],
                Asset.ConsensusFromGovernance(10));
            _states = _states.MintAsset(
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                ValidatorAddresses[1],
                Asset.ConsensusFromGovernance(30));
            _states = _states.MintAsset(
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                ValidatorAddresses[2],
                Asset.ConsensusFromGovernance(50));
            _states = _states.MintAsset(
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                ValidatorAddresses[3],
                Asset.ConsensusFromGovernance(40));
            _states = _states.MintAsset(
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                ValidatorAddresses[4],
                Asset.ConsensusFromGovernance(20));
            _states = ValidatorPowerIndexCtrl.Update(_states, ValidatorAddresses);
            ValidatorPowerIndex validatorPowerIndex;
            (_states, validatorPowerIndex)
                = ValidatorPowerIndexCtrl.FetchValidatorPowerIndex(_states);
            List<ValidatorPower> index = validatorPowerIndex.Index.ToList();
            Assert.Equal(5, index.Count);
            Assert.Equal(ValidatorAddresses[2], index[0].ValidatorAddress);
            Assert.Equal(Asset.ConsensusFromGovernance(60), index[0].ConsensusToken);
            Assert.Equal(ValidatorAddresses[3], index[1].ValidatorAddress);
            Assert.Equal(Asset.ConsensusFromGovernance(50), index[1].ConsensusToken);
            Assert.Equal(ValidatorAddresses[1], index[2].ValidatorAddress);
            Assert.Equal(Asset.ConsensusFromGovernance(40), index[2].ConsensusToken);
            Assert.Equal(ValidatorAddresses[4], index[3].ValidatorAddress);
            Assert.Equal(Asset.ConsensusFromGovernance(30), index[3].ConsensusToken);
            Assert.Equal(ValidatorAddresses[0], index[4].ValidatorAddress);
            Assert.Equal(Asset.ConsensusFromGovernance(20), index[4].ConsensusToken);
        }

        [Fact]
        public void SortingTestSameToken()
        {
            (_states, _) = ValidatorPowerIndexCtrl.FetchValidatorPowerIndex(_states);
            _states = _states.MintAsset(
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                ValidatorAddresses[0],
                Asset.ConsensusFromGovernance(10));
            _states = _states.MintAsset(
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                ValidatorAddresses[1],
                Asset.ConsensusFromGovernance(10));
            _states = _states.MintAsset(
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                ValidatorAddresses[2],
                Asset.ConsensusFromGovernance(10));
            _states = _states.MintAsset(
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                ValidatorAddresses[3],
                Asset.ConsensusFromGovernance(10));
            _states = _states.MintAsset(
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                ValidatorAddresses[4],
                Asset.ConsensusFromGovernance(10));
            _states = ValidatorPowerIndexCtrl.Update(_states, ValidatorAddresses);
            ValidatorPowerIndex validatorPowerIndex;
            (_states, validatorPowerIndex)
                = ValidatorPowerIndexCtrl.FetchValidatorPowerIndex(_states);
            List<ValidatorPower> index = validatorPowerIndex.Index.ToList();
            Assert.Equal(5, index.Count);
            for (int i = 0; i < index.Count - 1; i++)
            {
                Assert.True(((IComparable<Address>)index[i].ValidatorAddress)
                    .CompareTo(index[i + 1].ValidatorAddress) > 0);
            }
        }
    }
}
