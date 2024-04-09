namespace Lib9c.Tests.Action.DPoS.Control
{
    using System;
    using System.Linq;
    using System.Numerics;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action.DPoS.Control;
    using Nekoyume.Action.DPoS.Exception;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Action.DPoS.Model;
    using Nekoyume.Module;
    using Xunit;

    public class SlashCtrlTest : PoSTest
    {
        public static readonly object[][] TestData = new object[][]
        {
            new object[] { false, new BigInteger(20) },
            new object[] { true, new BigInteger(20) },
        };

        private const int ValidatorCount = 2;
        private const int DelegatorCount = 2;

        private readonly PublicKey[] _operatorPublicKeys;
        private readonly Address[] _operatorAddresses;
        private readonly Address[] _delegatorAddresses;
        private readonly Address[] _validatorAddresses;
        private readonly FungibleAssetValue _defaultNCG
            = new FungibleAssetValue(Asset.GovernanceToken, 1, 0);

        private readonly BigInteger _slashFactor = new BigInteger(20);
        private IWorld _states;

        public SlashCtrlTest()
        {
            _operatorPublicKeys = Enumerable.Range(0, ValidatorCount)
                .Select(_ => new PrivateKey().PublicKey)
                .ToArray();
            _operatorAddresses = _operatorPublicKeys.Select(item => item.Address).ToArray();
            _delegatorAddresses = Enumerable.Range(0, DelegatorCount)
                .Select(_ => CreateAddress())
                .ToArray();
            _validatorAddresses = _operatorAddresses
                .Select(item => Validator.DeriveAddress(item))
                .ToArray();
            _states = InitializeStates();
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void Slash_Test(bool jailed, BigInteger slashFactor)
        {
            var validatorNCG = _defaultNCG;
            var consensusToken = Asset.ConsensusFromGovernance(validatorNCG);

            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var states = _states;

            // Promote validator with 1 NCG
            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                governanceToken: validatorNCG);
            states = Update(
                states: states,
                blockIndex: 1);

            var bondedPoolNCG = states.GetBalance(ReservedAddress.BondedPool, Asset.GovernanceToken);
            if (jailed)
            {
                states = Jail(
                    states: states,
                    validatorAddress: validatorAddress);
            }

            // Expect to slash 5 from validator's power and send 0.05 NCG to community pool
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 3, },
                validatorAddress: validatorAddress,
                infractionHeight: 1,
                power: consensusToken.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Expect 95 ConsensusToken in validator
            var expectedConsensusToken = SlashAsset(consensusToken, slashFactor);
            // Expect 100 Share in validator
            var expectedShare = GetShare(states, validatorAddress, expectedConsensusToken);
            // Expect 0.95 NCG in bonded pool
            var expectedBondedPoolNCG = bondedPoolNCG - (validatorNCG - SlashAsset(validatorNCG, slashFactor));
            // Expect 0.05 NCG in community pool
            var expectedCommunityPoolNCG = validatorNCG - expectedBondedPoolNCG;

            var actualShare = GetShare(states, validatorAddress);
            var actualConsensusToken = GetPower(states, validatorAddress);
            var actualBondedPoolNCG = states.GetBalance(ReservedAddress.BondedPool, Asset.GovernanceToken);
            var actualCommunityPoolNCG = states.GetBalance(ReservedAddress.CommunityPool, Asset.GovernanceToken);

            Assert.Equal(expectedShare, actualShare);
            Assert.Equal(expectedConsensusToken, actualConsensusToken);
            Assert.Equal(expectedBondedPoolNCG, actualBondedPoolNCG);
            Assert.Equal(expectedCommunityPoolNCG, actualCommunityPoolNCG);

            _states = states;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Slash_WithDelegation_Test(bool jailed)
        {
            var validatorNCG = _defaultNCG;
            var delegatorNCG = _defaultNCG;
            var consensusToken = Asset.ConsensusFromGovernance(validatorNCG + delegatorNCG);
            var slashFactor = _slashFactor;

            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var delegatorAddress = _delegatorAddresses[0];
            var states = _states;

            // Promote validator with 1 NCG
            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                governanceToken: validatorNCG);
            // Delegate 1 NCG by delegator
            states = Delegate(
                states: states,
                blockIndex: 1,
                delegatorAddress: delegatorAddress,
                validatorAddress: validatorAddress,
                governanceToken: validatorNCG);
            states = Update(
                states: states,
                blockIndex: 1);

            var bondedPoolNCG = states.GetBalance(ReservedAddress.BondedPool, Asset.GovernanceToken);
            if (jailed)
            {
                states = Jail(
                    states: states,
                    validatorAddress: validatorAddress);
            }

            // Expect to slash 10 from validator's power and send 0.1 NCG to community pool
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 2, },
                validatorAddress: validatorAddress,
                infractionHeight: 1,
                power: consensusToken.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Expect 200 Share in validator
            var expectedShare = FungibleAssetValue.FromRawValue(Asset.Share, consensusToken.RawValue);
            // Expect 190 ConsensusToken in validator
            var expectedConsensusToken = SlashAsset(consensusToken, slashFactor);
            // Expect 1.9 NCG in bonded pool
            var expectedBondedPoolNCG = SlashAsset(validatorNCG + delegatorNCG, slashFactor);
            // Expect 0.1 NCG in community pool
            var expectedCommunityPoolNCG = bondedPoolNCG - expectedBondedPoolNCG;

            var actualShare = GetShare(states, validatorAddress);
            var actualConsensusToken = GetPower(states, validatorAddress);
            var actualBondedPoolNCG = states.GetBalance(ReservedAddress.BondedPool, Asset.GovernanceToken);
            var actualCommunityPoolNCG = states.GetBalance(ReservedAddress.CommunityPool, Asset.GovernanceToken);

            Assert.Equal(expectedShare, actualShare);
            Assert.Equal(expectedConsensusToken, actualConsensusToken);
            Assert.Equal(expectedBondedPoolNCG, actualBondedPoolNCG);
            Assert.Equal(expectedCommunityPoolNCG, actualCommunityPoolNCG);

            _states = states;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Slash_WithUndelegation_Test(bool jailed)
        {
            var validatorNCG = _defaultNCG;
            var delegatorNCG = _defaultNCG;
            var slashFactor = _slashFactor;
            var delegatorConsensusPower = Asset.ConsensusFromGovernance(delegatorNCG);
            var undelegationShare = FungibleAssetValue.FromRawValue(
                currency: Asset.Share,
                rawValue: delegatorConsensusPower.RawValue);

            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var delegatorAddress = _delegatorAddresses[0];
            var states = _states;

            // Promote validator with 1 NCG
            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                governanceToken: validatorNCG);
            states = Update(states, blockIndex: 1);

            // Delegate 1 NCG by delegator
            states = Delegate(
                states: states,
                blockIndex: 2,
                delegatorAddress: delegatorAddress,
                validatorAddress: validatorAddress,
                governanceToken: delegatorNCG);
            states = Update(states, blockIndex: 2);

            var consensusTokenBeforeInfraction = GetPower(states, validatorAddress);

            // Undelegate 100 Share by delegator
            states = Undelegate(
                states: states,
                blockIndex: 3,
                delegatorAddress: delegatorAddress,
                validatorAddress: validatorAddress,
                share: undelegationShare
            );
            states = Update(states, blockIndex: 3);

            var bondedPoolNCG = states.GetBalance(ReservedAddress.BondedPool, Asset.GovernanceToken);
            var unbondedPoolNCG = states.GetBalance(ReservedAddress.UnbondedPool, Asset.GovernanceToken);
            var consensusToken = GetPower(states, validatorAddress);
            var share = GetShare(states, validatorAddress);

            if (jailed)
            {
                states = Jail(
                    states: states,
                    validatorAddress: validatorAddress);
            }

            // Expect to slash 10 from validator's power and send 0.1 NCG to community pool
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 4, },
                validatorAddress: validatorAddress,
                infractionHeight: 2,
                power: consensusTokenBeforeInfraction.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Expect 100 Share in validator
            var expectedShare = share;
            // Expect 95 ConsensusToken in validator
            var expectedConsensusToken = SlashAsset(consensusToken, slashFactor);
            // Expect 0.95 NCG in bonded pool
            var expectedBondedPoolNCG = SlashAsset(validatorNCG, slashFactor);
            // Expect 0.95 NCG in unbonded pool
            var expectedUnbondedPoolNCG = SlashAsset(validatorNCG, slashFactor);
            // Expect 0.1 NCG in community pool
            var expectedCommunityPoolNCG = bondedPoolNCG + unbondedPoolNCG - expectedBondedPoolNCG - expectedUnbondedPoolNCG;

            var undelegation = UndelegateCtrl.GetUndelegation(states, delegatorAddress, validatorAddress);
            var undelegationEntry = UndelegateCtrl.GetUndelegationEntry(states, undelegation.UndelegationEntryAddresses[0]);

            Assert.NotNull(undelegation);
            Assert.NotNull(undelegationEntry);
            Assert.Equal(expectedConsensusToken, undelegationEntry.UnbondingConsensusToken);

            var actualShare = GetShare(states, validatorAddress);
            var actualConsensusToken = GetPower(states, validatorAddress);
            var actualBondedPoolNCG = states.GetBalance(ReservedAddress.BondedPool, Asset.GovernanceToken);
            var actualUnbondedPoolNCG = states.GetBalance(ReservedAddress.UnbondedPool, Asset.GovernanceToken);
            var actualCommunityPoolNCG = states.GetBalance(ReservedAddress.CommunityPool, Asset.GovernanceToken);

            Assert.Equal(expectedShare, actualShare);
            Assert.Equal(expectedConsensusToken, actualConsensusToken);
            Assert.Equal(expectedBondedPoolNCG, actualBondedPoolNCG);
            Assert.Equal(expectedUnbondedPoolNCG, actualUnbondedPoolNCG);
            Assert.Equal(expectedCommunityPoolNCG, actualCommunityPoolNCG);

            _states = states;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Slash_OnlyValidator_AfterUndelegating_Test(bool jailed)
        {
            var validatorNCG = _defaultNCG;
            var delegatorNCG = _defaultNCG;
            var slashFactor = _slashFactor;
            var delegatorConsensusPower = Asset.ConsensusFromGovernance(delegatorNCG);
            var undelegationShare = FungibleAssetValue.FromRawValue(
                currency: Asset.Share,
                rawValue: delegatorConsensusPower.RawValue);

            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var delegatorAddress = _delegatorAddresses[0];
            var states = _states;

            // Promote validator with 1 NCG
            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                governanceToken: validatorNCG);
            states = Update(states, blockIndex: 1);

            // Delegate 1 NCG by delegator
            states = Delegate(
                states: states,
                blockIndex: 2,
                delegatorAddress: delegatorAddress,
                validatorAddress: validatorAddress,
                governanceToken: delegatorNCG);
            states = Update(states, blockIndex: 2);

            // Undelegate 100 Share by delegator
            states = Undelegate(
                states: states,
                blockIndex: 3,
                delegatorAddress: delegatorAddress,
                validatorAddress: validatorAddress,
                share: undelegationShare);
            states = Update(states, blockIndex: 3);

            var consensusTokenBeforeInfraction = GetPower(states, validatorAddress);
            var bondedPoolNCG = states.GetBalance(ReservedAddress.BondedPool, Asset.GovernanceToken);
            var unbondedPoolNCG = states.GetBalance(ReservedAddress.UnbondedPool, Asset.GovernanceToken);
            var consensusToken = GetPower(states, validatorAddress);
            var share = GetShare(states, validatorAddress);

            if (jailed)
            {
                states = Jail(
                    states: states,
                    validatorAddress: validatorAddress);
            }

            // Expect to slash only 5 from the validator's power, excluding the delegator, at height 4.
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 4, },
                validatorAddress: validatorAddress,
                infractionHeight: 4,
                power: consensusTokenBeforeInfraction.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Expect 100 Share in validator
            var expectedShare = share;
            // Expect 95 ConsensusToken in validator
            var expectedConsensusToken = SlashAsset(consensusToken, slashFactor);
            // Expect 0.95 NCG in bonded pool
            var expectedBondedPoolNCG = SlashAsset(validatorNCG, slashFactor);
            // Expect 1.0 NCG in unbonded pool
            var expectedUnbondedPoolNCG = delegatorNCG;
            // Expect 0.05 NCG in community pool
            var expectedCommunityPoolNCG = bondedPoolNCG + unbondedPoolNCG - expectedBondedPoolNCG - expectedUnbondedPoolNCG;

            var undelegation = UndelegateCtrl.GetUndelegation(states, delegatorAddress, validatorAddress);
            var undelegationEntry = UndelegateCtrl.GetUndelegationEntry(states, undelegation.UndelegationEntryAddresses[0]);

            Assert.NotNull(undelegation);
            Assert.NotNull(undelegationEntry);
            Assert.Equal(
                expected: Asset.ConsensusFromGovernance(delegatorNCG),
                actual: undelegationEntry.UnbondingConsensusToken);

            var actualShare = GetShare(states, validatorAddress);
            var actualConsensusToken = GetPower(states, validatorAddress);
            var actualBondedPoolNCG = states.GetBalance(ReservedAddress.BondedPool, Asset.GovernanceToken);
            var actualUnbondedPoolNCG = states.GetBalance(ReservedAddress.UnbondedPool, Asset.GovernanceToken);
            var actualCommunityPoolNCG = states.GetBalance(ReservedAddress.CommunityPool, Asset.GovernanceToken);

            Assert.Equal(expectedShare, actualShare);
            Assert.Equal(expectedConsensusToken, actualConsensusToken);
            Assert.Equal(expectedBondedPoolNCG, actualBondedPoolNCG);
            Assert.Equal(expectedUnbondedPoolNCG, actualUnbondedPoolNCG);
            Assert.Equal(expectedCommunityPoolNCG, actualCommunityPoolNCG);

            _states = states;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Slash_WithRedelegation_Test(bool jailed)
        {
            var slashFactor = _slashFactor;

            var srcOperatorPublicKey = _operatorPublicKeys[0];
            var dstOperatorPublicKey = _operatorPublicKeys[1];
            var srcValidatorAddress = _validatorAddresses[0];
            var dstValidatorAddress = _validatorAddresses[1];
            var delegatorAddress = _delegatorAddresses[0];
            var states = _states;
            var srcValidatorNCG = _defaultNCG;
            var dstValidatorNCG = _defaultNCG;
            var delegatorNCG = _defaultNCG;
            var delegatorConsensusPower = Asset.ConsensusFromGovernance(delegatorNCG);
            var redelegationShare = FungibleAssetValue.FromRawValue(
                currency: Asset.Share,
                rawValue: delegatorConsensusPower.RawValue);

            // Delegate 100 NCG by src operator
            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: srcOperatorPublicKey,
                governanceToken: srcValidatorNCG);
            // Delegate 100 NCG by dst operator
            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: dstOperatorPublicKey,
                governanceToken: dstValidatorNCG);
            states = Update(states, blockIndex: 1);

            // Delgate 100 NCG by delegator to src validator
            states = Delegate(
                states: states,
                blockIndex: 2,
                delegatorAddress: delegatorAddress,
                validatorAddress: srcValidatorAddress,
                governanceToken: delegatorNCG);
            states = Update(states, blockIndex: 2);
            var consensusTokenBeforeInfraction = GetPower(states, srcValidatorAddress);

            // Redelegate 100 NCG from src validator to dst validator
            states = Redelegate(
                states: states,
                blockIndex: 3,
                delegatorAddress: delegatorAddress,
                srcValidatorAddress: srcValidatorAddress,
                dstValidatorAddress: dstValidatorAddress,
                share: redelegationShare);
            states = Update(states, blockIndex: 3);

            // 3 NCG in bonded pool
            var bondedPoolNCG = states.GetBalance(ReservedAddress.BondedPool, Asset.GovernanceToken);
            // 0 NCG in unbonded pool
            var unbondedPoolNCG = states.GetBalance(ReservedAddress.UnbondedPool, Asset.GovernanceToken);
            // 100 consensus token in src validator
            var srcConsensusToken = GetPower(states, srcValidatorAddress);
            // 200 consensus token in dst validator
            var dstConsensusToken = GetPower(states, dstValidatorAddress);

            // 100 share in src validator
            var srcShare = GetShare(states, srcValidatorAddress);
            // 200 share in dst validator
            var dstShare = GetShare(states, dstValidatorAddress);

            if (jailed)
            {
                states = Jail(
                    states: states,
                    validatorAddress: srcValidatorAddress);
            }

            // Slash src validator at height 2
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 4, },
                validatorAddress: srcValidatorAddress,
                infractionHeight: 2,
                power: consensusTokenBeforeInfraction.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Expect 95 ConsensusToken in src validator
            var expectedSrcConsensusToken = SlashAsset(srcConsensusToken, slashFactor);
            // Expect 195 ConsensusToken in dst validator
            var expectedDstConsensusToken =
                Asset.ConsensusFromGovernance(dstValidatorNCG) +
                SlashAsset(Asset.ConsensusFromGovernance(delegatorNCG), slashFactor);
            // Expect 100 Share in src validator
            var expectedSrcShare = srcShare;
            // Expect 195 Shre in dst validator
            var expectedDstShare =
                ValidatorCtrl.ShareFromConsensusToken(states, dstValidatorAddress, expectedDstConsensusToken);
            // Expect 2.9 NCG in bonded pool
            var expectedBondedPoolNCG = SlashAsset(srcValidatorNCG + delegatorNCG, slashFactor) + dstValidatorNCG;
            // Expect 0 NCG in unbonded pool
            var expectedUnbondedPoolNCG = new FungibleAssetValue(Asset.GovernanceToken, 0, 0);
            // Expect 0.1 NCG in community pool
            var expectedCommunityPoolNCG = srcValidatorNCG + dstValidatorNCG + delegatorNCG - expectedBondedPoolNCG;

            var redelegation = RedelegateCtrl.GetRedelegation(states, delegatorAddress, srcValidatorAddress, dstValidatorAddress);
            var redelegationEntry = RedelegateCtrl.GetRedelegationEntry(states, redelegation.RedelegationEntryAddresses[0]);

            Assert.NotNull(redelegation);
            Assert.NotNull(redelegationEntry);

            var actualSrcConsensusToken = GetPower(states, srcValidatorAddress);
            var actualDstConsensusToken = GetPower(states, dstValidatorAddress);
            var actualSrcShare = GetShare(states, srcValidatorAddress);
            var actualDstShare = GetShare(states, dstValidatorAddress);
            var actualBondedPoolNCG = states.GetBalance(ReservedAddress.BondedPool, Asset.GovernanceToken);
            var actualUnbondedPoolNCG = states.GetBalance(ReservedAddress.UnbondedPool, Asset.GovernanceToken);
            var actualCommunityPoolNCG = states.GetBalance(ReservedAddress.CommunityPool, Asset.GovernanceToken);

            Assert.Equal(expectedSrcShare, actualSrcShare);
            Assert.Equal(expectedDstShare, actualDstShare);
            Assert.Equal(expectedSrcConsensusToken, actualSrcConsensusToken);
            Assert.Equal(expectedDstConsensusToken, actualDstConsensusToken);
            Assert.Equal(expectedBondedPoolNCG, actualBondedPoolNCG);
            Assert.Equal(expectedUnbondedPoolNCG, actualUnbondedPoolNCG);
            Assert.Equal(expectedCommunityPoolNCG, actualCommunityPoolNCG);

            _states = states;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Slash_OnlyValidator_AfterRedelegating_Test(bool jailed)
        {
            var governanceToken = _defaultNCG;
            var slashFactor = _slashFactor;

            var srcOperatorPublicKey = _operatorPublicKeys[0];
            var dstOperatorPublicKey = _operatorPublicKeys[1];
            var srcValidatorAddress = _validatorAddresses[0];
            var dstValidatorAddress = _validatorAddresses[1];
            var delegatorAddress = _delegatorAddresses[0];
            var states = _states;
            var srcValidatorNCG = governanceToken;
            var dstValidatorNCG = governanceToken;
            var delegatorNCG = governanceToken;
            var delegatorConsensusPower = Asset.ConsensusFromGovernance(delegatorNCG);

            // Promote src validator with 1 NCG
            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: srcOperatorPublicKey,
                governanceToken: srcValidatorNCG);
            // Promote dst validator with 1 NCG
            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: dstOperatorPublicKey,
                governanceToken: dstValidatorNCG);
            states = Update(states, blockIndex: 1);

            // Delgate 1 NCG by delegator to src validator
            states = Delegate(
                states: states,
                blockIndex: 2,
                delegatorAddress: delegatorAddress,
                validatorAddress: srcValidatorAddress,
                governanceToken: delegatorNCG);
            states = Update(states, blockIndex: 2);

            // Redelegate 100 share from src validator to dst validator
            states = Redelegate(
                states: states,
                blockIndex: 3,
                delegatorAddress: delegatorAddress,
                srcValidatorAddress: srcValidatorAddress,
                dstValidatorAddress: dstValidatorAddress,
                share: FungibleAssetValue.FromRawValue(Asset.Share, delegatorConsensusPower.RawValue));
            states = Update(states, blockIndex: 3);

            var consensusTokenBeforeInfraction = GetPower(states, srcValidatorAddress);
            // 3 NCG in bonded pool
            var bondedPoolNCG = states.GetBalance(ReservedAddress.BondedPool, Asset.GovernanceToken);
            // 0 NCG in unbonded pool
            var unbondedPoolNCG = states.GetBalance(ReservedAddress.UnbondedPool, Asset.GovernanceToken);
            // 100 consensus token in src validator
            var srcConsensusToken = GetPower(states, srcValidatorAddress);
            // 200 consensus token in dst validator
            var dstConsensusToken = GetPower(states, dstValidatorAddress);

            // 100 share in src validator
            var srcShare = GetShare(states, srcValidatorAddress);
            // 200 share in dst validator
            var dstShare = GetShare(states, dstValidatorAddress);

            if (jailed)
            {
                states = Jail(
                    states: states,
                    validatorAddress: srcValidatorAddress);
            }

            // Slash only src validator's power at height 4
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 4, },
                validatorAddress: srcValidatorAddress,
                infractionHeight: 4,
                power: consensusTokenBeforeInfraction.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Expect 95 ConsensusToken in src validator
            var expectedSrcConsensusToken = SlashAsset(srcConsensusToken, slashFactor);
            // Expect 200 ConsensusToken in src validator
            var expectedDstConsensusToken =
                Asset.ConsensusFromGovernance(dstValidatorNCG) +
                Asset.ConsensusFromGovernance(delegatorNCG);
            // Expect 100 Share in src validator
            var expectedSrcShare = srcShare;
            // Expect 200 Share in dst validator
            var expectedDstShare = FungibleAssetValue.FromRawValue(Asset.Share, expectedDstConsensusToken.RawValue);
            // Expect 2.95 NCG in bonded pool
            var expectedBondedPoolNCG = SlashAsset(srcValidatorNCG, slashFactor) + delegatorNCG + dstValidatorNCG;
            // Expect 0 NCG in unbonded pool
            var expectedUnbondedPoolNCG = new FungibleAssetValue(Asset.GovernanceToken, 0, 0);
            // Expect 0.05 NCG in community pool
            var expectedCommunityPoolNCG = srcValidatorNCG + dstValidatorNCG + delegatorNCG - expectedBondedPoolNCG;

            var redelegation = RedelegateCtrl.GetRedelegation(states, delegatorAddress, srcValidatorAddress, dstValidatorAddress);
            var redelegationEntry = RedelegateCtrl.GetRedelegationEntry(states, redelegation.RedelegationEntryAddresses[0]);

            Assert.NotNull(redelegation);
            Assert.NotNull(redelegationEntry);

            var actualSrcConsensusToken = GetPower(states, srcValidatorAddress);
            var actualDstConsensusToken = GetPower(states, dstValidatorAddress);
            var actualSrcShare = GetShare(states, srcValidatorAddress);
            var actualDstShare = GetShare(states, dstValidatorAddress);
            var actualBondedPoolNCG = states.GetBalance(ReservedAddress.BondedPool, Asset.GovernanceToken);
            var actualUnbondedPoolNCG = states.GetBalance(ReservedAddress.UnbondedPool, Asset.GovernanceToken);
            var actualCommunityPoolNCG = states.GetBalance(ReservedAddress.CommunityPool, Asset.GovernanceToken);

            Assert.Equal(expectedSrcShare, actualSrcShare);
            Assert.Equal(expectedDstShare, actualDstShare);
            Assert.Equal(expectedSrcConsensusToken, actualSrcConsensusToken);
            Assert.Equal(expectedDstConsensusToken, actualDstConsensusToken);
            Assert.Equal(expectedBondedPoolNCG, actualBondedPoolNCG);
            Assert.Equal(expectedUnbondedPoolNCG, actualUnbondedPoolNCG);
            Assert.Equal(expectedCommunityPoolNCG, actualCommunityPoolNCG);

            _states = states;
        }

        [Fact]
        public void Slash_InvalidValidatorAddress_FailTest()
        {
            var states = _states;
            var validatorAddress = CreateAddress();

            Assert.Throws<NullValidatorException>(() =>
            {
                SlashCtrl.Slash(
                    world: states,
                    actionContext: new ActionContext { PreviousState = states, },
                    validatorAddress: validatorAddress,
                    infractionHeight: 2,
                    power: 100,
                    slashFactor: 20,
                    nativeTokens: NativeTokens);
            });
        }

        [Fact]
        public void Slash_NegativeSlashFactor_FailTest()
        {
            var states = _states;
            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];

            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                governanceToken: new FungibleAssetValue(Asset.GovernanceToken, 100, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SlashCtrl.Slash(
                    world: states,
                    actionContext: new ActionContext { PreviousState = states, },
                    validatorAddress: validatorAddress,
                    infractionHeight: 2,
                    power: 100,
                    slashFactor: -1,
                    nativeTokens: NativeTokens);
            });
        }

        [Fact]
        public void Slash_FutureBlockHeight_FailTest()
        {
            var validatorNCG = _defaultNCG;
            var consensusToken = Asset.ConsensusFromGovernance(validatorNCG);
            var slashFactor = _slashFactor;

            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var states = _states;

            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                governanceToken: validatorNCG);
            states = Update(
                states: states,
                blockIndex: 1);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SlashCtrl.Slash(
                    world: states,
                    actionContext: new ActionContext { PreviousState = states, BlockIndex = 2, },
                    validatorAddress: validatorAddress,
                    infractionHeight: 3,
                    power: consensusToken.RawValue,
                    slashFactor: slashFactor,
                    nativeTokens: NativeTokens);
            });
        }

        [Fact]
        public void Unjail_Test()
        {
            var validatorNCG = _defaultNCG;
            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var states = _states;

            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                governanceToken: validatorNCG);
            states = Update(
                states: states,
                blockIndex: 1);
            states = Jail(
                states: states,
                validatorAddress: validatorAddress);

            states = SlashCtrl.Unjail(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 2, },
                validatorAddress: validatorAddress);

            var actualValidator = ValidatorCtrl.GetValidator(states, validatorAddress);
            Assert.False(actualValidator!.Jailed);

            _states = states;
        }

        [Fact]
        public void Unjail_NotPromotedValidator_FailTest()
        {
            var states = _states;
            var validatorAddress = _validatorAddresses[0];

            Assert.Throws<NullValidatorException>(() =>
            {
                SlashCtrl.Unjail(
                    world: states,
                    actionContext: new ActionContext { PreviousState = states, BlockIndex = 2, },
                    validatorAddress: validatorAddress);
            });
        }

        [Fact]
        public void Unjail_NotJailedValidator_FailTest()
        {
            var validatorNCG = _defaultNCG;
            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var states = _states;

            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                governanceToken: validatorNCG);

            Assert.Throws<InvalidOperationException>(() =>
            {
                SlashCtrl.Unjail(
                    world: states,
                    actionContext: new ActionContext { PreviousState = states, BlockIndex = 2, },
                    validatorAddress: validatorAddress);
            });
        }

        [Fact]
        public void Unjail_Tombstoned_FailTest()
        {
            var validatorNCG = _defaultNCG;
            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var states = _states;

            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                governanceToken: validatorNCG);
            states = Tombstone(
                states: states,
                validatorAddress: validatorAddress);

            Assert.Throws<InvalidOperationException>(() =>
            {
                SlashCtrl.Unjail(
                    world: states,
                    actionContext: new ActionContext { PreviousState = states, BlockIndex = 2, },
                    validatorAddress: validatorAddress);
            });
        }

        [Fact]
        public void Unjail_StillJailed_FailTest()
        {
            var validatorNCG = _defaultNCG;
            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var states = _states;

            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                governanceToken: validatorNCG);
            states = Update(
                states: states,
                blockIndex: 1);
            states = JailUntil(
                states: states,
                validatorAddress: validatorAddress,
                blockHeight: long.MaxValue);

            Assert.Throws<InvalidOperationException>(() =>
            {
                SlashCtrl.Unjail(
                    world: states,
                    actionContext: new ActionContext { PreviousState = states, BlockIndex = 1, },
                    validatorAddress: validatorAddress);
            });
        }

        [Fact]
        public void Unjail_PowerIsLessThanMinimum_FailTest()
        {
            var validatorNCG = _defaultNCG;
            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var states = _states;

            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                governanceToken: validatorNCG);
            states = Update(
                states: states,
                blockIndex: 1);
            states = Slash(
                states: states,
                blockIndex: 2,
                validatorAddress: validatorAddress,
                infractionHeight: 1,
                power: Asset.ConsensusFromGovernance(validatorNCG).RawValue,
                slashFactor: 2);
            states = Jail(
                states: states,
                validatorAddress: validatorAddress);

            Assert.Throws<InvalidOperationException>(() =>
            {
                SlashCtrl.Unjail(
                    world: states,
                    actionContext: new ActionContext { PreviousState = states, BlockIndex = 2, },
                    validatorAddress: validatorAddress);
            });
        }

        private static FungibleAssetValue SlashAsset(FungibleAssetValue value, BigInteger factor)
        {
            var (amount, _) = value.DivRem(factor);
            return value - amount;
        }

        private static FungibleAssetValue GetPower(IWorldState worldState, Address validatorAddress)
        {
            return worldState.GetBalance(
                address: validatorAddress,
                currency: Asset.ConsensusToken);
        }

        private static FungibleAssetValue GetShare(IWorldState worldState, Address validatorAddress)
        {
            var validator = ValidatorCtrl.GetValidator(worldState, validatorAddress)!;
            return validator.DelegatorShares;
        }

        private static FungibleAssetValue GetShare(
            IWorldState worldState,
            Address validatorAddress,
            FungibleAssetValue consensusToken)
        {
            var share = ValidatorCtrl.ShareFromConsensusToken(
                worldState,
                validatorAddress,
                consensusToken);
            return share ?? new FungibleAssetValue(Asset.Share);
        }
    }
}
