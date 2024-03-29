namespace Lib9c.Tests.Action.DPoS
{
    using System;
    using Libplanet.Crypto;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Action.DPoS.Model;
    using Xunit;

    public class ValidatorPowerComparerTest : PoSTest
    {
        [Fact]
        public void CompareDifferentTokenTest()
        {
            PublicKey publicKeyA = new PrivateKey().PublicKey;
            PublicKey publicKeyB = new PrivateKey().PublicKey;
            ValidatorPower validatorPowerA = new ValidatorPower(
                publicKeyA.Address, publicKeyA, Asset.ConsensusToken * 10);
            ValidatorPower validatorPowerB = new ValidatorPower(
                publicKeyB.Address, publicKeyB, Asset.ConsensusToken * 11);
            Assert.True(((IComparable<ValidatorPower>)validatorPowerA)
                .CompareTo(validatorPowerB) > 0);
        }

        [Fact]
        public void CompareSameTokenTest()
        {
            PublicKey publicKeyA = new PrivateKey().PublicKey;
            PublicKey publicKeyB = new PrivateKey().PublicKey;
            ValidatorPower validatorPowerA = new ValidatorPower(
                publicKeyA.Address, publicKeyA, Asset.ConsensusToken * 10);
            ValidatorPower validatorPowerB = new ValidatorPower(
                publicKeyB.Address, publicKeyB, Asset.ConsensusToken * 10);
            int sign = -((IComparable<Address>)publicKeyA.Address)
                .CompareTo(publicKeyB.Address);
            Assert.True(((IComparable<ValidatorPower>)validatorPowerA)
                .CompareTo(validatorPowerB) == sign);
        }
    }
}
