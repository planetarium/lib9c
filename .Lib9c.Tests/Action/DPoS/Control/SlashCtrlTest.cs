namespace Lib9c.Tests.Action.DPoS.Control
{
    using System;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action.DPoS.Control;
    using Nekoyume.Action.DPoS.Exception;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Action.DPoS.Model;
    using Xunit;

    public class SlashCtrlTest : SlashCtrlTestBase
    {
        private const int ValidatorCount = 2;
        private const int DelegatorCount = 2;

        private readonly PublicKey[] _operatorPublicKeys;
        private readonly Address[] _operatorAddresses;
        private readonly Address[] _delegatorAddresses;
        private readonly Address[] _validatorAddresses;
        private readonly FungibleAssetValue _defaultNCG
            = new FungibleAssetValue(Asset.GovernanceToken, 1, 0);

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
        [InlineData(false, 20)]
        [InlineData(false, 3)]
        [InlineData(false, 1)]
        [InlineData(true, 20)]
        [InlineData(true, 3)]
        [InlineData(true, 1)]
        public void Slash_Test(bool jailed, int slashFactor)
        {
            // Given
            var validatorNCG = _defaultNCG;
            var validatorPower = PowerFromNCG(validatorNCG);
            var validatorShare = ShareFromPower(validatorPower);

            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var states = _states;

            // Promote validator with 1 NCG at height 1
            using (var blockIndex = new BlockIndex(1))
            {
                states = Promote(
                    states: states,
                    blockIndex: blockIndex,
                    operatorPublicKey: operatorPublicKey,
                    ncg: validatorNCG);
                states = Update(states: states, blockIndex: blockIndex);
            }

            states = JailIf(states, validatorAddress, condition: jailed);

            // When (if factor is 20)
            // Expect to slash 5 power from validator and send 0.05 NCG to community pool.
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 3, },
                validatorAddress: validatorAddress,
                infractionHeight: 1,
                power: validatorPower.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Then (if factor is 20)
            // Expect 95 Power in validator
            var expectedPower = SlashPower(validatorPower, slashFactor);
            // Expect 100 Share in validator
            var expectedShare = validatorShare;
            // Expect 0.95 NCG in bonded pool
            var expectedBondedPoolNCG = SlashNCG(validatorNCG, slashFactor);
            // Expect 0 NCG in unbonded pool
            var expectedUnbondedPoolNCG = ZeroNCG;
            // Expect 0.05 NCG in community pool
            var expectedCommunityPoolNCG = GetSlashAmount(validatorNCG, slashFactor);
            // Expect 1 NCG
            var expectedTotalNCG = validatorNCG;

            var actualShare = GetShare(states, validatorAddress);
            var actualPower = GetPower(states, validatorAddress);
            var actualBondedPoolNCG = GetNCG(states, ReservedAddress.BondedPool);
            var actualUnbondedPoolNCG = GetNCG(states, ReservedAddress.UnbondedPool);
            var actualCommunityPoolNCG = GetNCG(states, ReservedAddress.CommunityPool);
            var actualTotalNCG = actualBondedPoolNCG + actualUnbondedPoolNCG + actualCommunityPoolNCG;

            Assert.Equal(expectedShare, actualShare);
            Assert.Equal(expectedPower, actualPower);
            Assert.Equal(expectedBondedPoolNCG, actualBondedPoolNCG);
            Assert.Equal(expectedUnbondedPoolNCG, actualUnbondedPoolNCG);
            Assert.Equal(expectedCommunityPoolNCG, actualCommunityPoolNCG);
            Assert.Equal(expectedTotalNCG, actualTotalNCG);

            _states = states;
        }

        [Theory]
        [InlineData(false, 20)]
        [InlineData(false, 3)]
        [InlineData(false, 1)]
        [InlineData(true, 20)]
        [InlineData(true, 3)]
        [InlineData(true, 1)]
        public void Slash_WithDelegation_Test(bool jailed, int slashFactor)
        {
            // Given
            var validatorNCG = _defaultNCG;
            var delegatorNCG = _defaultNCG;

            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var delegatorAddress = _delegatorAddresses[0];
            var states = _states;

            // Promote validator and Delegate with 1 NCG each at height 1
            using (var blockIndex = new BlockIndex(1))
            {
                states = Promote(
                    states: states,
                    blockIndex: blockIndex,
                    operatorPublicKey: operatorPublicKey,
                    ncg: validatorNCG);
                states = Delegate(
                    states: states,
                    blockIndex: blockIndex,
                    validatorAddress: validatorAddress,
                    delegatorAddress: delegatorAddress,
                    ncg: validatorNCG);
                states = Update(states: states, blockIndex: blockIndex);
            }

            var validatorPower = GetPower(states, validatorAddress);
            var validatorShare = GetShare(states, validatorAddress);

            states = JailIf(states, validatorAddress, condition: jailed);

            // When (if factor is 20)
            // Expect to slash 10 power from validator and send 0.1 NCG to community pool.
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 2, },
                validatorAddress: validatorAddress,
                infractionHeight: 1,
                power: validatorPower.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Then (if factor is 20)
            // Expect 200 Share in validator
            var expectedShare = validatorShare;
            // Expect 190 Power in validator
            var expectedPower = SlashPower(validatorPower, slashFactor);
            // Expect 1.9 NCG in bonded pool
            var expectedBondedPoolNCG =
                SlashNCG(validatorNCG, slashFactor, assetsToSlashFirst: delegatorNCG) +
                SlashNCG(delegatorNCG, slashFactor);
            // Expect 0 NCG in bonded pool
            var expectedUnbondedPoolNCG = ZeroNCG;
            // Expect 0.1 NCG in community pool
            var expectedCommunityPoolNCG =
                GetSlashAmount(validatorNCG, slashFactor, assetsToSlashFirst: delegatorNCG) +
                GetSlashAmount(delegatorNCG, slashFactor);
            // Expect 2 NCG
            var expectedTotalNCG = validatorNCG + delegatorNCG;

            var actualShare = GetShare(states, validatorAddress);
            var actualPower = GetPower(states, validatorAddress);
            var actualBondedPoolNCG = GetNCG(states, ReservedAddress.BondedPool);
            var actualUnbondedPoolNCG = GetNCG(states, ReservedAddress.UnbondedPool);
            var actualCommunityPoolNCG = GetNCG(states, ReservedAddress.CommunityPool);
            var actualTotalNCG = actualBondedPoolNCG + actualUnbondedPoolNCG + actualCommunityPoolNCG;

            Assert.Equal(expectedShare, actualShare);
            Assert.Equal(expectedPower, actualPower);
            Assert.Equal(expectedBondedPoolNCG, actualBondedPoolNCG);
            Assert.Equal(expectedUnbondedPoolNCG, actualUnbondedPoolNCG);
            Assert.Equal(expectedCommunityPoolNCG, actualCommunityPoolNCG);
            Assert.Equal(expectedTotalNCG, actualTotalNCG);

            _states = states;
        }

        [Theory]
        [InlineData(false, 20)]
        [InlineData(false, 3)]
        [InlineData(false, 1)]
        [InlineData(true, 20)]
        [InlineData(true, 3)]
        [InlineData(true, 1)]
        public void Slash_WithUndelegation_Test(bool jailed, int slashFactor)
        {
            // Given
            var validatorNCG = _defaultNCG;
            var delegatorNCG = _defaultNCG;
            var delegatorPower = PowerFromNCG(delegatorNCG);
            var delegatorShare = ShareFromPower(delegatorPower);

            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var delegatorAddress = _delegatorAddresses[0];
            var states = _states;

            // Promote validator with 1 NCG at height 1
            using (var blockIndex = new BlockIndex(1))
            {
                states = Promote(
                    states: states,
                    blockIndex: blockIndex,
                    operatorPublicKey: operatorPublicKey,
                    ncg: validatorNCG);
                states = Update(states: states, blockIndex: blockIndex);
            }

            // Delegate 1 NCG by delegator at height 2
            using (var blockIndex = new BlockIndex(2))
            {
                states = Delegate(
                    states: states,
                    blockIndex: blockIndex,
                    validatorAddress: validatorAddress,
                    delegatorAddress: delegatorAddress,
                    ncg: delegatorNCG);
                states = Update(states: states, blockIndex: blockIndex);
            }

            var powerBeforeInfraction = GetPower(states, validatorAddress);

            // Undelegate 100 Share by delegator at height 3
            using (var blockIndex = new BlockIndex(3))
            {
                states = Undelegate(
                    states: states,
                    blockIndex: blockIndex,
                    validatorAddress: validatorAddress,
                    delegatorAddress: delegatorAddress,
                    share: delegatorShare
                );
                states = Update(states: states, blockIndex: blockIndex);
            }

            var validatorPower = GetPower(states, validatorAddress);
            var validatorShare = GetShare(states, validatorAddress);

            states = JailIf(states, validatorAddress, condition: jailed);

            // When (if factor is 20)
            // Expect to slash 10 power from validator and send 0.1 NCG to community pool.
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 4, },
                validatorAddress: validatorAddress,
                infractionHeight: 2,
                power: powerBeforeInfraction.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Then (if factor is 20)
            // Expect 100 Share in validator
            var expectedShare = validatorShare;
            // Expect 95 Power in validator
            var expectedPower = SlashPower(validatorPower, slashFactor, assetsToSlashFirst: delegatorPower);
            // Expect 0.95 NCG in bonded pool
            var expectedBondedPoolNCG = SlashNCG(validatorNCG, slashFactor, assetsToSlashFirst: delegatorNCG);
            // Expect 0.95 NCG in unbonded pool
            var expectedUnbondedPoolNCG = SlashNCG(validatorNCG, slashFactor);
            // Expect 0.1 NCG in community pool
            var expectedCommunityPoolNCG =
                GetSlashAmount(validatorNCG, slashFactor, assetsToSlashFirst: delegatorNCG) +
                GetSlashAmount(delegatorNCG, slashFactor);
            // Expect 2 NCG in community pool
            var expectedTotalNCG = validatorNCG + delegatorNCG;

            var actualShare = GetShare(states, validatorAddress);
            var actualPower = GetPower(states, validatorAddress);
            var actualBondedPoolNCG = GetNCG(states, ReservedAddress.BondedPool);
            var actualUnbondedPoolNCG = GetNCG(states, ReservedAddress.UnbondedPool);
            var actualCommunityPoolNCG = GetNCG(states, ReservedAddress.CommunityPool);
            var actualTotalNCG = actualBondedPoolNCG + actualUnbondedPoolNCG + actualCommunityPoolNCG;

            Assert.Equal(expectedShare, actualShare);
            Assert.Equal(expectedPower, actualPower);
            Assert.Equal(expectedBondedPoolNCG, actualBondedPoolNCG);
            Assert.Equal(expectedUnbondedPoolNCG, actualUnbondedPoolNCG);
            Assert.Equal(expectedCommunityPoolNCG, actualCommunityPoolNCG);
            Assert.Equal(expectedTotalNCG, actualTotalNCG);

            _states = states;
        }

        [Theory]
        [InlineData(false, 20)]
        [InlineData(false, 3)]
        [InlineData(false, 1)]
        [InlineData(true, 20)]
        [InlineData(true, 3)]
        [InlineData(true, 1)]
        public void Slash_OnlyValidator_AfterUndelegating_Test(bool jailed, int slashFactor)
        {
            // Given
            var validatorNCG = _defaultNCG;
            var delegatorNCG = _defaultNCG;
            var delegatorPower = PowerFromNCG(delegatorNCG);
            var delegatorShare = ShareFromPower(delegatorPower);

            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var delegatorAddress = _delegatorAddresses[0];
            var states = _states;

            // Promote validator with 1 NCG at height 1
            using (var blockIndex = new BlockIndex(1))
            {
                states = Promote(
                    states: states,
                    blockIndex: blockIndex,
                    operatorPublicKey: operatorPublicKey,
                    ncg: validatorNCG);
                states = Update(states: states, blockIndex: blockIndex);
            }

            // Delegate 1 NCG by delegator at height 2
            using (var blockIndex = new BlockIndex(2))
            {
                states = Delegate(
                    states: states,
                    blockIndex: blockIndex,
                    validatorAddress: validatorAddress,
                    delegatorAddress: delegatorAddress,
                    ncg: delegatorNCG);
                states = Update(states: states, blockIndex: blockIndex);
            }

            // Undelegate 100 Share by delegator at height 3
            using (var blockIndex = new BlockIndex(3))
            {
                states = Undelegate(
                    states: states,
                    blockIndex: blockIndex,
                    validatorAddress: validatorAddress,
                    delegatorAddress: delegatorAddress,
                    share: delegatorShare);
                states = Update(states: states, blockIndex: blockIndex);
            }

            var powerBeforeInfraction = GetPower(states, validatorAddress);
            var validatorPower = GetPower(states, validatorAddress);
            var validatorShare = GetShare(states, validatorAddress);

            states = JailIf(states, validatorAddress, condition: jailed);

            // When (if factor is 20)
            // Expect to slash only 5 power from validator and send 0.05 NCG to community pool.
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 4, },
                validatorAddress: validatorAddress,
                infractionHeight: 4,
                power: powerBeforeInfraction.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Then (if factor is 20)
            // Expect 100 Share in validator
            var expectedShare = validatorShare;
            // Expect 95 Power in validator
            var expectedPower = SlashPower(validatorPower, slashFactor);
            // Expect 0.95 NCG in bonded pool
            var expectedBondedPoolNCG = SlashNCG(validatorNCG, slashFactor);
            // Expect 1.0 NCG in unbonded pool
            var expectedUnbondedPoolNCG = delegatorNCG;
            // Expect 0.05 NCG in community pool
            var expectedCommunityPoolNCG = GetSlashAmount(validatorNCG, slashFactor);
            // Expect 2 NCG
            var expectedTotalNCG = validatorNCG + delegatorNCG;

            var actualShare = GetShare(states, validatorAddress);
            var actualPower = GetPower(states, validatorAddress);
            var actualBondedPoolNCG = GetNCG(states, ReservedAddress.BondedPool);
            var actualUnbondedPoolNCG = GetNCG(states, ReservedAddress.UnbondedPool);
            var actualCommunityPoolNCG = GetNCG(states, ReservedAddress.CommunityPool);
            var actualTotalNCG = actualBondedPoolNCG + actualUnbondedPoolNCG + actualCommunityPoolNCG;

            Assert.Equal(expectedShare, actualShare);
            Assert.Equal(expectedPower, actualPower);
            Assert.Equal(expectedBondedPoolNCG, actualBondedPoolNCG);
            Assert.Equal(expectedUnbondedPoolNCG, actualUnbondedPoolNCG);
            Assert.Equal(expectedCommunityPoolNCG, actualCommunityPoolNCG);
            Assert.Equal(expectedTotalNCG, actualTotalNCG);

            _states = states;
        }

        [Theory]
        [InlineData(false, 20)]
        [InlineData(false, 3)]
        [InlineData(false, 1)]
        [InlineData(true, 20)]
        [InlineData(true, 3)]
        [InlineData(true, 1)]
        public void Slash_WithRedelegation_Test(bool jailed, int slashFactor)
        {
            // Given
            var srcValidatorNCG = _defaultNCG;
            var dstValidatorNCG = _defaultNCG;
            var delegatorNCG = _defaultNCG;
            var delegatorPower = PowerFromNCG(delegatorNCG);
            var delegatorShare = ShareFromPower(delegatorPower);

            var srcOperatorPublicKey = _operatorPublicKeys[0];
            var dstOperatorPublicKey = _operatorPublicKeys[1];
            var srcValidatorAddress = _validatorAddresses[0];
            var dstValidatorAddress = _validatorAddresses[1];
            var delegatorAddress = _delegatorAddresses[0];
            var states = _states;

            // Promote src validator and dst validator with 1 NCG each at height 1
            using (var blockIndex = new BlockIndex(1))
            {
                states = Promote(
                    states: states,
                    blockIndex: blockIndex,
                    operatorPublicKey: srcOperatorPublicKey,
                    ncg: srcValidatorNCG);
                // Delegate 100 NCG by dst operator
                states = Promote(
                    states: states,
                    blockIndex: blockIndex,
                    operatorPublicKey: dstOperatorPublicKey,
                    ncg: dstValidatorNCG);
                states = Update(states: states, blockIndex: blockIndex);
            }

            // Delgate 100 NCG by delegator to src validator at height 2
            using (var blockIndex = new BlockIndex(2))
            {
                states = Delegate(
                    states: states,
                    blockIndex: blockIndex,
                    validatorAddress: srcValidatorAddress,
                    delegatorAddress: delegatorAddress,
                    ncg: delegatorNCG);
                states = Update(states: states, blockIndex: blockIndex);
            }

            var powerBeforeInfraction = GetPower(states, srcValidatorAddress);

            // Redelegate 100 NCG from src validator to dst validator at height 3
            using (var blockIndex = new BlockIndex(3))
            {
                states = Redelegate(
                    states: states,
                    blockIndex: blockIndex,
                    srcValidatorAddress: srcValidatorAddress,
                    dstValidatorAddress: dstValidatorAddress,
                    delegatorAddress: delegatorAddress,
                    share: delegatorShare);
                states = Update(states: states, blockIndex: blockIndex);
            }

            var srcPower = GetPower(states, srcValidatorAddress);
            var srcShare = GetShare(states, srcValidatorAddress);

            states = JailIf(states, srcValidatorAddress, condition: jailed);

            // When (if factor is 20)
            // Expect to slash 5 from the src validator's power.
            // Expect to slash 5 from the dst validator's power.
            // Expect to send 0.1 NCG(src 0.05 + dst 0.05) to community pool.
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 4, },
                validatorAddress: srcValidatorAddress,
                infractionHeight: 2,
                power: powerBeforeInfraction.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Then (if factor is 20)
            // Expect 95 Power in src validator
            var expectedSrcPower = SlashPower(srcPower, slashFactor, assetsToSlashFirst: delegatorPower);
            // Expect 195 Power in dst validator
            var expectedDstPower =
                PowerFromNCG(dstValidatorNCG) +
                SlashPower(delegatorPower, slashFactor);
            // Expect 100 Share in src validator
            var expectedSrcShare = srcShare;
            // Expect 195 Shre in dst validator
            var expectedDstShare = ShareFromPower(expectedDstPower);
            // Expect 2.9 NCG in bonded pool
            var expectedBondedPoolNCG = SlashNCG(srcValidatorNCG + delegatorNCG, slashFactor) + dstValidatorNCG;
            // Expect 0 NCG in unbonded pool
            var expectedUnbondedPoolNCG = ZeroNCG;
            // Expect 0.1 NCG in community pool
            var expectedCommunityPoolNCG =
                GetSlashAmount(srcValidatorNCG, slashFactor, assetsToSlashFirst: delegatorNCG) +
                GetSlashAmount(delegatorNCG, slashFactor);
            // Expect 3 NCG
            var expectedTotalNCG = srcValidatorNCG + dstValidatorNCG + delegatorNCG;

            var actualSrcPower = GetPower(states, srcValidatorAddress);
            var actualDstPower = GetPower(states, dstValidatorAddress);
            var actualSrcShare = GetShare(states, srcValidatorAddress);
            var actualDstShare = GetShare(states, dstValidatorAddress);
            var actualBondedPoolNCG = GetNCG(states, ReservedAddress.BondedPool);
            var actualUnbondedPoolNCG = GetNCG(states, ReservedAddress.UnbondedPool);
            var actualCommunityPoolNCG = GetNCG(states, ReservedAddress.CommunityPool);
            var actualTotalNCG = actualBondedPoolNCG + actualUnbondedPoolNCG + actualCommunityPoolNCG;

            Assert.Equal(expectedSrcShare, actualSrcShare);
            Assert.Equal(expectedDstShare, actualDstShare);
            Assert.Equal(expectedSrcPower, actualSrcPower);
            Assert.Equal(expectedDstPower, actualDstPower);
            Assert.Equal(expectedBondedPoolNCG, actualBondedPoolNCG);
            Assert.Equal(expectedUnbondedPoolNCG, actualUnbondedPoolNCG);
            Assert.Equal(expectedCommunityPoolNCG, actualCommunityPoolNCG);
            Assert.Equal(expectedTotalNCG, actualTotalNCG);

            _states = states;
        }

        [Theory]
        [InlineData(false, 20)]
        [InlineData(false, 3)]
        [InlineData(false, 1)]
        [InlineData(true, 20)]
        [InlineData(true, 3)]
        [InlineData(true, 1)]
        public void Slash_OnlyValidator_AfterRedelegating_Test(bool jailed, int slashFactor)
        {
            // Given
            var srcValidatorNCG = _defaultNCG;
            var dstValidatorNCG = _defaultNCG;
            var delegatorNCG = _defaultNCG;
            var delegatorPower = PowerFromNCG(delegatorNCG);
            var delegatorShare = ShareFromPower(delegatorPower);

            var srcOperatorPublicKey = _operatorPublicKeys[0];
            var dstOperatorPublicKey = _operatorPublicKeys[1];
            var srcValidatorAddress = _validatorAddresses[0];
            var dstValidatorAddress = _validatorAddresses[1];
            var delegatorAddress = _delegatorAddresses[0];
            var states = _states;

            // Promote src validator and dst validator with 1 NCG each at height 1
            using (var blockIndex = new BlockIndex(1))
            {
                states = Promote(
                    states: states,
                    blockIndex: blockIndex,
                    operatorPublicKey: srcOperatorPublicKey,
                    ncg: srcValidatorNCG);
                states = Promote(
                    states: states,
                    blockIndex: blockIndex,
                    operatorPublicKey: dstOperatorPublicKey,
                    ncg: dstValidatorNCG);
                states = Update(states: states, blockIndex: blockIndex);
            }

            // Delgate 1 NCG by delegator to src validator at height 2
            using (var blockIndex = new BlockIndex(2))
            {
                states = Delegate(
                    states: states,
                    blockIndex: blockIndex,
                    validatorAddress: srcValidatorAddress,
                    delegatorAddress: delegatorAddress,
                    ncg: delegatorNCG);
                states = Update(states: states, blockIndex: blockIndex);
            }

            // Redelegate 100 share from src validator to dst validator at height 3
            using (var blockIndex = new BlockIndex(3))
            {
                states = Redelegate(
                    states: states,
                    blockIndex: blockIndex,
                    srcValidatorAddress: srcValidatorAddress,
                    dstValidatorAddress: dstValidatorAddress,
                    delegatorAddress: delegatorAddress,
                    share: delegatorShare);
                states = Update(states: states, blockIndex: blockIndex);
            }

            var powerBeforeInfraction = GetPower(states, srcValidatorAddress);
            var srcPower = GetPower(states, srcValidatorAddress);
            var srcShare = GetShare(states, srcValidatorAddress);

            states = JailIf(states, srcValidatorAddress, condition: jailed);

            // When (if factor is 20)
            // Expect to slash only 5 from the src validator's power.
            // Expect to send 0.05 NCG to community pool.
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 4, },
                validatorAddress: srcValidatorAddress,
                infractionHeight: 4,
                power: powerBeforeInfraction.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Then (if factor is 20)
            // Expect 95 Power in src validator
            var expectedSrcPower = SlashPower(srcPower, slashFactor);
            // Expect 200 Power in src validator
            var expectedDstPower = PowerFromNCG(dstValidatorNCG) + PowerFromNCG(delegatorNCG);
            // Expect 100 Share in src validator
            var expectedSrcShare = srcShare;
            // Expect 200 Share in dst validator
            var expectedDstShare = ShareFromPower(expectedDstPower);
            // Expect 2.95 NCG in bonded pool
            var expectedBondedPoolNCG = SlashNCG(srcValidatorNCG, slashFactor) + delegatorNCG + dstValidatorNCG;
            // Expect 0 NCG in unbonded pool
            var expectedUnbondedPoolNCG = ZeroNCG;
            // Expect 0.05 NCG in community pool
            var expectedCommunityPoolNCG = GetSlashAmount(srcValidatorNCG, slashFactor);
            // Expect 3 NCG
            var expectedTotalNCG = srcValidatorNCG + dstValidatorNCG + delegatorNCG;

            var actualSrcPower = GetPower(states, srcValidatorAddress);
            var actualDstPower = GetPower(states, dstValidatorAddress);
            var actualSrcShare = GetShare(states, srcValidatorAddress);
            var actualDstShare = GetShare(states, dstValidatorAddress);
            var actualBondedPoolNCG = GetNCG(states, ReservedAddress.BondedPool);
            var actualUnbondedPoolNCG = GetNCG(states, ReservedAddress.UnbondedPool);
            var actualCommunityPoolNCG = GetNCG(states, ReservedAddress.CommunityPool);
            var actualTotalNCG = actualBondedPoolNCG + actualUnbondedPoolNCG + actualCommunityPoolNCG;

            Assert.Equal(expectedSrcShare, actualSrcShare);
            Assert.Equal(expectedDstShare, actualDstShare);
            Assert.Equal(expectedSrcPower, actualSrcPower);
            Assert.Equal(expectedDstPower, actualDstPower);
            Assert.Equal(expectedBondedPoolNCG, actualBondedPoolNCG);
            Assert.Equal(expectedUnbondedPoolNCG, actualUnbondedPoolNCG);
            Assert.Equal(expectedCommunityPoolNCG, actualCommunityPoolNCG);
            Assert.Equal(expectedTotalNCG, actualTotalNCG);

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

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void Slash_NegativeSlashFactor_FailTest(int slashFactor)
        {
            var states = _states;
            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];

            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                ncg: new FungibleAssetValue(Asset.GovernanceToken, 100, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SlashCtrl.Slash(
                    world: states,
                    actionContext: new ActionContext { PreviousState = states, },
                    validatorAddress: validatorAddress,
                    infractionHeight: 2,
                    power: 100,
                    slashFactor: slashFactor,
                    nativeTokens: NativeTokens);
            });
        }

        [Fact]
        public void Slash_FutureBlockHeight_FailTest()
        {
            var validatorNCG = _defaultNCG;
            var consensusToken = Asset.ConsensusFromGovernance(validatorNCG);

            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var states = _states;

            states = Promote(
                states: states,
                blockIndex: 1,
                operatorPublicKey: operatorPublicKey,
                ncg: validatorNCG);
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
                    slashFactor: 20,
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
                ncg: validatorNCG);
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
                ncg: validatorNCG);

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
                ncg: validatorNCG);
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
                ncg: validatorNCG);
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
                ncg: validatorNCG);
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
    }
}
