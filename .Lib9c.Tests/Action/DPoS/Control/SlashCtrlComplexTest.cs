namespace Lib9c.Tests.Action.DPoS.Control
{
    using System;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action.DPoS.Control;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Action.DPoS.Model;
    using Xunit;

    public class SlashCtrlComplexTest : SlashCtrlTestBase
    {
        public static readonly object[][] TestData
            = Enumerable.Range(0, Random.Shared.Next(5, 10))
                        .Select(item => new object[]
                        {
                            Random.Shared.Next() % 2 == 0, // jailed
                            Random.Shared.Next(1, 101), // slashFactor
                        })
                        .ToArray();

        private const int ValidatorCount = 2;
        private static readonly int DelegatorCount = Random.Shared.Next(2, 32);

        private readonly PublicKey[] _operatorPublicKeys;
        private readonly Address[] _operatorAddresses;
        private readonly Address[] _delegatorAddresses;
        private readonly Address[] _validatorAddresses;

        private IWorld _states;

        public SlashCtrlComplexTest()
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
        public void Slash_Test(bool jailed, int slashFactor)
        {
            // Given
            var validatorNCG = NextNCG();
            var validatorPower = PowerFromNCG(validatorNCG);
            var validatorShare = ShareFromPower(validatorPower);

            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var states = _states;

            // Promote validator at height 1
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

            // When
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 3, },
                validatorAddress: validatorAddress,
                infractionHeight: 1,
                power: validatorPower.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Then
            var expectedPower = SlashPower(validatorPower, slashFactor);
            var expectedShare = validatorShare;
            var expectedBondedPoolNCG = SlashNCG(validatorNCG, slashFactor);
            var expectedUnbondedPoolNCG = ZeroNCG;
            var expectedCommunityPoolNCG = GetSlashAmount(validatorNCG, slashFactor);
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
        [MemberData(nameof(TestData))]
        public void Slash_WithDelegation_Test(bool jailed, int slashFactor)
        {
            // Given
            var validatorNCG = NextNCG();
            var delegatorNCGs = NextNCGMany(length: DelegatorCount);

            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var delegatorAddresses = _delegatorAddresses;
            var states = _states;

            // Promote validator and Delegate many at height 1
            using (var blockIndex = new BlockIndex(1))
            {
                states = Promote(
                    states: states,
                    blockIndex: blockIndex,
                    operatorPublicKey: operatorPublicKey,
                    ncg: validatorNCG);
                states = DelegateMany(
                    states: states,
                    blockIndex: blockIndex,
                    validatorAddress: validatorAddress,
                    delegatorAddresses: delegatorAddresses,
                    ncgs: delegatorNCGs);
                states = Update(states: states, blockIndex: blockIndex);
            }

            var validatorPower = GetPower(states, validatorAddress);
            var validatorShare = GetShare(states, validatorAddress);

            states = JailIf(states, validatorAddress, condition: jailed);

            // When
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 2, },
                validatorAddress: validatorAddress,
                infractionHeight: 1,
                power: validatorPower.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Then
            var expectedShare = validatorShare;
            var expectedPower = SlashPower(validatorPower, slashFactor);
            var expectedBondedPoolNCG =
                SlashNCG(validatorNCG, slashFactor, assetsToSlashFirst: delegatorNCGs) +
                SumNCG(SlashNCGs(delegatorNCGs, slashFactor));
            var expectedUnbondedPoolNCG = ZeroNCG;
            var expectedCommunityPoolNCG =
                GetSlashAmount(validatorNCG, slashFactor, assetsToSlashFirst: delegatorNCGs) +
                SumNCG(GetSlashAmounts(delegatorNCGs, slashFactor));
            var expectedTotalNCG = validatorNCG + SumNCG(delegatorNCGs);

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
        [MemberData(nameof(TestData))]
        public void Slash_WithUndelegation_Test(bool jailed, int slashFactor)
        {
            // Given
            var validatorNCG = NextNCG();
            var delegatorNCGs = NextNCGMany(length: DelegatorCount);
            var delegatorPowers = PowerFromNCG(delegatorNCGs);
            var delegatorShares = ShareFromPower(delegatorPowers);

            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var delegatorAddresses = _delegatorAddresses;
            var states = _states;

            // Promote validator at height 1
            using (var blockIndex = new BlockIndex(1))
            {
                states = Promote(
                    states: states,
                    blockIndex: blockIndex,
                    operatorPublicKey: operatorPublicKey,
                    ncg: validatorNCG);
                states = Update(states: states, blockIndex: blockIndex);
            }

            // Delegate many at height 2
            using (var blockIndex = new BlockIndex(2))
            {
                states = DelegateMany(
                    states: states,
                    blockIndex: blockIndex,
                    validatorAddress: validatorAddress,
                    delegatorAddresses: delegatorAddresses,
                    ncgs: delegatorNCGs);
                states = Update(states: states, blockIndex: blockIndex);
            }

            var powerBeforeInfraction = GetPower(states, validatorAddress);

            // Undelegate many at height 3
            using (var blockIndex = new BlockIndex(3))
            {
                states = UndelegateMany(
                    states: states,
                    blockIndex: blockIndex,
                    validatorAddress: validatorAddress,
                    delegatorAddresses: delegatorAddresses,
                    shares: delegatorShares);
                states = Update(states: states, blockIndex: blockIndex);
            }

            var validatorPower = GetPower(states, validatorAddress);
            var validatorShare = GetShare(states, validatorAddress);

            states = JailIf(states, validatorAddress, condition: jailed);

            // When
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 4, },
                validatorAddress: validatorAddress,
                infractionHeight: 2,
                power: powerBeforeInfraction.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Then
            var expectedShare = validatorShare;
            var expectedPower = SlashPower(validatorPower, slashFactor, assetsToSlashFirst: delegatorPowers);
            var expectedBondedPoolNCG = SlashNCG(validatorNCG, slashFactor, assetsToSlashFirst: delegatorNCGs);
            var expectedUnbondedPoolNCG = SumNCG(SlashNCGs(delegatorNCGs, slashFactor));
            var expectedCommunityPoolNCG =
                GetSlashAmount(validatorNCG, slashFactor, assetsToSlashFirst: delegatorNCGs) +
                SumNCG(GetSlashAmounts(delegatorNCGs, slashFactor));
            var expectedTotalNCG = validatorNCG + SumNCG(delegatorNCGs);

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
        [MemberData(nameof(TestData))]
        public void Slash_OnlyValidator_AfterUndelegating_Test(bool jailed, int slashFactor)
        {
            // Given
            var validatorNCG = NextNCG();
            var delegatorNCGs = NextNCGMany(length: DelegatorCount);
            var delegatorPowers = PowerFromNCG(delegatorNCGs);
            var delegatorShares = ShareFromPower(delegatorPowers);

            var operatorPublicKey = _operatorPublicKeys[0];
            var validatorAddress = _validatorAddresses[0];
            var delegatorAddresses = _delegatorAddresses;
            var states = _states;

            // Promote validator at height 1
            using (var blockIndex = new BlockIndex(1))
            {
                states = Promote(
                    states: states,
                    blockIndex: blockIndex,
                    operatorPublicKey: operatorPublicKey,
                    ncg: validatorNCG);
                states = Update(states: states, blockIndex: blockIndex);
            }

            // Delegate many at height 2
            using (var blockIndex = new BlockIndex(2))
            {
                states = DelegateMany(
                    states: states,
                    blockIndex: blockIndex,
                    validatorAddress: validatorAddress,
                    delegatorAddresses: delegatorAddresses,
                    ncgs: delegatorNCGs);
                states = Update(states: states, blockIndex: blockIndex);
            }

            // Undelegate many at height 3
            using (var blockIndex = new BlockIndex(3))
            {
                states = UndelegateMany(
                    states: states,
                    blockIndex: blockIndex,
                    validatorAddress: validatorAddress,
                    delegatorAddresses: delegatorAddresses,
                    shares: delegatorShares);
                states = Update(states: states, blockIndex: blockIndex);
            }

            var powerBeforeInfraction = GetPower(states, validatorAddress);
            var validatorPower = GetPower(states, validatorAddress);
            var validatorShare = GetShare(states, validatorAddress);

            states = JailIf(states, validatorAddress, condition: jailed);

            // When
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 4, },
                validatorAddress: validatorAddress,
                infractionHeight: 4,
                power: powerBeforeInfraction.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Then
            var expectedShare = validatorShare;
            var expectedPower = SlashPower(validatorPower, slashFactor);
            var expectedBondedPoolNCG = SlashNCG(validatorNCG, slashFactor);
            var expectedUnbondedPoolNCG = SumNCG(delegatorNCGs);
            var expectedCommunityPoolNCG = GetSlashAmount(validatorNCG, slashFactor);
            var expectedTotalNCG = validatorNCG + SumNCG(delegatorNCGs);

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
        [MemberData(nameof(TestData))]
        public void Slash_WithRedelegation_Test(bool jailed, int slashFactor)
        {
            // Given
            var srcValidatorNCG = NextNCG();
            var dstValidatorNCG = NextNCG();
            var delegatorNCGs = NextNCGMany(length: DelegatorCount);
            var delegatorPowers = PowerFromNCG(delegatorNCGs);
            var delegatorShares = ShareFromPower(delegatorPowers);

            var srcOperatorPublicKey = _operatorPublicKeys[0];
            var dstOperatorPublicKey = _operatorPublicKeys[1];
            var srcValidatorAddress = _validatorAddresses[0];
            var dstValidatorAddress = _validatorAddresses[1];
            var delegatorAddresses = _delegatorAddresses;
            var states = _states;

            // Promote src validator and dst validator at height 1
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

            // Delegate many at height 2
            using (var blockIndex = new BlockIndex(2))
            {
                states = DelegateMany(
                    states: states,
                    blockIndex: blockIndex,
                    validatorAddress: srcValidatorAddress,
                    delegatorAddresses: delegatorAddresses,
                    ncgs: delegatorNCGs);
                states = Update(states: states, blockIndex: blockIndex);
            }

            var powerBeforeInfraction = GetPower(states, srcValidatorAddress);

            // Redelegate many at height 3
            using (var blockIndex = new BlockIndex(3))
            {
                states = RedelegateMany(
                    states: states,
                    blockIndex: blockIndex,
                    srcValidatorAddress: srcValidatorAddress,
                    dstValidatorAddress: dstValidatorAddress,
                    delegatorAddresses: delegatorAddresses,
                    shares: delegatorShares);
                states = Update(states: states, blockIndex: blockIndex);
            }

            var srcPower = GetPower(states, srcValidatorAddress);
            var srcShare = GetShare(states, srcValidatorAddress);

            states = JailIf(states, srcValidatorAddress, condition: jailed);

            // When
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 4, },
                validatorAddress: srcValidatorAddress,
                infractionHeight: 2,
                power: powerBeforeInfraction.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Then
            var expectedSrcPower = SlashPower(srcPower, slashFactor, assetsToSlashFirst: delegatorPowers);
            var expectedDstPower =
                PowerFromNCG(dstValidatorNCG) +
                SumPower(SlashPowers(delegatorPowers, slashFactor));
            var expectedSrcShare = srcShare;
            var expectedDstShare = ShareFromPower(expectedDstPower);
            var expectedBondedPoolNCG =
                SlashNCG(srcValidatorNCG, slashFactor, assetsToSlashFirst: delegatorNCGs) +
                SumNCG(SlashNCGs(delegatorNCGs, slashFactor)) +
                dstValidatorNCG;
            var expectedUnbondedPoolNCG = ZeroNCG;
            var expectedCommunityPoolNCG =
                GetSlashAmount(srcValidatorNCG, slashFactor, assetsToSlashFirst: delegatorNCGs) +
                SumNCG(GetSlashAmounts(delegatorNCGs, slashFactor));
            var expectedTotalNCG = srcValidatorNCG + dstValidatorNCG + SumNCG(delegatorNCGs);

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
        [MemberData(nameof(TestData))]
        public void Slash_OnlyValidator_AfterRedelegating_Test(bool jailed, int slashFactor)
        {
            // Given
            var srcValidatorNCG = NextNCG();
            var dstValidatorNCG = NextNCG();
            var delegatorNCGs = NextNCGMany(length: DelegatorCount);
            var delegatorPowers = PowerFromNCG(delegatorNCGs);
            var delegatorShares = ShareFromPower(delegatorPowers);

            var srcOperatorPublicKey = _operatorPublicKeys[0];
            var dstOperatorPublicKey = _operatorPublicKeys[1];
            var srcValidatorAddress = _validatorAddresses[0];
            var dstValidatorAddress = _validatorAddresses[1];
            var delegatorAddresses = _delegatorAddresses;
            var states = _states;

            // Promote src validator and dest validator at height 1
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

            // Delegate many to src validator at height 2
            using (var blockIndex = new BlockIndex(2))
            {
                states = DelegateMany(
                    states: states,
                    blockIndex: blockIndex,
                    validatorAddress: srcValidatorAddress,
                    delegatorAddresses: delegatorAddresses,
                    ncgs: delegatorNCGs);
                states = Update(states: states, blockIndex: blockIndex);
            }

            // Redelegate many from src validator to dst validator at height 3
            using (var blockIndex = new BlockIndex(3))
            {
                states = RedelegateMany(
                    states: states,
                    blockIndex: blockIndex,
                    srcValidatorAddress: srcValidatorAddress,
                    dstValidatorAddress: dstValidatorAddress,
                    delegatorAddresses: delegatorAddresses,
                    shares: delegatorShares);
                states = Update(states: states, blockIndex: blockIndex);
            }

            var powerBeforeInfraction = GetPower(states, srcValidatorAddress);
            var srcPower = GetPower(states, srcValidatorAddress);
            var srcShare = GetShare(states, srcValidatorAddress);

            states = JailIf(states, srcValidatorAddress, condition: jailed);

            // When
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = 4, },
                validatorAddress: srcValidatorAddress,
                infractionHeight: 4,
                power: powerBeforeInfraction.RawValue,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);

            // Then
            var expectedSrcPower = SlashPower(srcPower, slashFactor);
            var expectedDstPower = PowerFromNCG(dstValidatorNCG) + SumPower(PowerFromNCG(delegatorNCGs));
            var expectedSrcShare = srcShare;
            var expectedDstShare = ShareFromPower(expectedDstPower);
            var expectedBondedPoolNCG = SlashNCG(srcValidatorNCG, slashFactor) + SumNCG(delegatorNCGs) + dstValidatorNCG;
            var expectedUnbondedPoolNCG = ZeroNCG;
            var expectedCommunityPoolNCG = srcValidatorNCG + dstValidatorNCG + SumNCG(delegatorNCGs) - expectedBondedPoolNCG;
            var expectedTotalNCG = srcValidatorNCG + dstValidatorNCG + SumNCG(delegatorNCGs);

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
    }
}
