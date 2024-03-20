using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Lib9c.DPoS.Control;
using Lib9c.DPoS.Misc;
using Lib9c.DPoS.Model;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Module;
using Xunit;

namespace Lib9c.DPoS.Tests.Control
{
    public class ValidatorSetCtrlTest : PoSTest
    {
        private ImmutableHashSet<Currency> _nativeTokens;
        private IWorld _states;

        public ValidatorSetCtrlTest()
            : base()
        {
            _nativeTokens = ImmutableHashSet.Create(
                Asset.GovernanceToken, Asset.ConsensusToken, Asset.Share);
            _states = InitializeStates();
            OperatorAddresses = new List<Address>();
            ValidatorAddresses = new List<Address>();
            DelegatorAddress = CreateAddress();
            _states = _states.MintAsset(
                new ActionContext
                {
                    PreviousState = _states,
                },
                DelegatorAddress,
                Asset.GovernanceToken * 100000);
            for (int i = 0; i < 200; i++)
            {
                PublicKey operatorPublicKey = new PrivateKey().PublicKey;
                Address operatorAddress = operatorPublicKey.Address;
                _states = _states.MintAsset(
                    new ActionContext
                    {
                        PreviousState = _states,
                    },
                    operatorAddress,
                    Asset.GovernanceToken * 1000);
                OperatorAddresses.Add(operatorAddress);
                _states = ValidatorCtrl.Create(
                    _states,
                    new ActionContext
                    {
                        PreviousState = _states,
                        BlockIndex = 1,
                    },
                    operatorAddress,
                    operatorPublicKey,
                    Asset.GovernanceToken * 1,
                    _nativeTokens);
                ValidatorAddresses.Add(Validator.DeriveAddress(operatorAddress));
            }
        }

        private List<Address> OperatorAddresses { get; set; }

        private List<Address> ValidatorAddresses { get; set; }

        private Address DelegatorAddress { get; set; }

        [Fact]
        public void ValidatorSetTest()
        {
            for (int i = 0; i < 200; i++)
            {
                _states = DelegateCtrl.Execute(
                    _states,
                    new ActionContext
                    {
                        PreviousState = _states,
                        BlockIndex = 1,
                    },
                    DelegatorAddress,
                    ValidatorAddresses[i],
                    Asset.GovernanceToken * (i + 1),
                    _nativeTokens);
            }

            Address validatorAddressA = ValidatorAddresses[3];
            Address validatorAddressB = ValidatorAddresses[5];
            Address delegationAddressB = Delegation.DeriveAddress(
                DelegatorAddress, validatorAddressB);

            _states = DelegateCtrl.Execute(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                DelegatorAddress,
                validatorAddressA,
                Asset.GovernanceToken * 200,
                _nativeTokens);

            _states = DelegateCtrl.Execute(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                },
                DelegatorAddress,
                validatorAddressB,
                Asset.GovernanceToken * 300,
                _nativeTokens);

            _states = ValidatorSetCtrl.Update(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                });

            ValidatorSet bondedSet;
            (_states, bondedSet) = ValidatorSetCtrl.FetchBondedValidatorSet(_states);
            Assert.Equal(
                validatorAddressB, bondedSet.Set.ToList()[0].ValidatorAddress);
            Assert.Equal(
                validatorAddressA, bondedSet.Set.ToList()[1].ValidatorAddress);
            Assert.Equal(
                Asset.Share * (5 + 1 + 300),
                _states.GetBalance(delegationAddressB, Asset.Share));
            Assert.Equal(
                Asset.ConsensusToken * (1 + 5 + 1 + 300),
                _states.GetBalance(ValidatorAddresses[5], Asset.ConsensusToken));
            Assert.Equal(
                Asset.GovernanceToken
                * (100 + (101 + 200) * 50 - 101 - 102 + 204 + 306),
                _states.GetBalance(ReservedAddress.BondedPool, Asset.GovernanceToken));
            Assert.Equal(
                Asset.GovernanceToken
                * (100 + (1 + 100) * 50 - 4 - 6 + 101 + 102),
                _states.GetBalance(ReservedAddress.UnbondedPool, Asset.GovernanceToken));
            Assert.Equal(
                Asset.GovernanceToken
                * (100000 - (1 + 200) * 100 - 200 - 300),
                _states.GetBalance(DelegatorAddress, Asset.GovernanceToken));

            _states = UndelegateCtrl.Execute(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 2,
                },
                DelegatorAddress,
                validatorAddressB,
                _states.GetBalance(delegationAddressB, Asset.Share),
                _nativeTokens);

            Assert.Equal(
                Asset.Share * 0,
                _states.GetBalance(delegationAddressB, Asset.Share));
            Assert.Equal(
                Asset.ConsensusToken * 1,
                _states.GetBalance(validatorAddressB, Asset.ConsensusToken));
            Assert.Equal(
                Asset.GovernanceToken
                * (100 + (101 + 200) * 50 - 101 - 102 + 204 + 306 - 306),
                _states.GetBalance(ReservedAddress.BondedPool, Asset.GovernanceToken));
            Assert.Equal(
                Asset.GovernanceToken
                * (100 + (1 + 100) * 50 - 4 - 6 + 101 + 102 + 306),
                _states.GetBalance(ReservedAddress.UnbondedPool, Asset.GovernanceToken));
            Assert.Equal(
                Asset.GovernanceToken
                * (100000 - (1 + 200) * 100 - 200 - 300),
                _states.GetBalance(DelegatorAddress, Asset.GovernanceToken));

            _states = ValidatorSetCtrl.Update(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 1,
                });
            (_states, bondedSet) = ValidatorSetCtrl.FetchBondedValidatorSet(_states);
            Assert.Equal(
                validatorAddressA, bondedSet.Set.ToList()[0].ValidatorAddress);
            Assert.Equal(
                Asset.GovernanceToken
                * (100 + (101 + 200) * 50 - 101 - 102 + 204 + 306 - 306 + 102),
                _states.GetBalance(ReservedAddress.BondedPool, Asset.GovernanceToken));
            Assert.Equal(
                Asset.GovernanceToken
                * (100 + (1 + 100) * 50 - 4 - 6 + 101 + 102 + 306 - 102),
                _states.GetBalance(ReservedAddress.UnbondedPool, Asset.GovernanceToken));
            Assert.Equal(
                Asset.GovernanceToken
                * (100000 - (1 + 200) * 100 - 200 - 300),
                _states.GetBalance(DelegatorAddress, Asset.GovernanceToken));

            _states = ValidatorSetCtrl.Update(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 50400 * 5,
                });

            Assert.Equal(
                Asset.GovernanceToken
                * (100 + (1 + 100) * 50 - 4 - 6 + 101 + 102 + 306 - 102 - 306),
                _states.GetBalance(ReservedAddress.UnbondedPool, Asset.GovernanceToken));
            Assert.Equal(
                Asset.GovernanceToken
                * (100000 - (1 + 200) * 100 - 200 - 300 + 306),
                _states.GetBalance(DelegatorAddress, Asset.GovernanceToken));
        }
    }
}
