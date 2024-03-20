using System;
using Lib9c.DPoS.Misc;
using Lib9c.DPoS.Model;
using Libplanet.Crypto;
using Xunit;

namespace Lib9c.DPoS.Tests
{
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
