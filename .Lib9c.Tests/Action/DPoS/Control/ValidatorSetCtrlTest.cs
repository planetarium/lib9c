namespace Lib9c.Tests.Action.DPoS.Control
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action.DPoS.Control;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Action.DPoS.Model;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class ValidatorSetCtrlTest : PoSTest
    {
        private readonly ImmutableHashSet<Currency> _nativeTokens;
        private IWorld _states;

        public ValidatorSetCtrlTest()
            : base()
        {
            _states = InitialState;
            _nativeTokens = NativeTokens;
            var governanceToken = _states.GetGoldCurrency();
            OperatorAddresses = new List<Address>();
            ValidatorAddresses = new List<Address>();
            DelegatorAddress = CreateAddress();
            _states = _states.TransferAsset(
                new ActionContext(),
                GoldCurrencyState.Address,
                DelegatorAddress,
                governanceToken * 100000);
            for (int i = 0; i < 200; i++)
            {
                PublicKey operatorPublicKey = new PrivateKey().PublicKey;
                Address operatorAddress = operatorPublicKey.Address;
                _states = _states.TransferAsset(
                    new ActionContext(),
                    GoldCurrencyState.Address,
                    operatorAddress,
                    governanceToken * 1000);
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
                    governanceToken * 1,
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
            var governanceToken = _states.GetGoldCurrency();
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
                    governanceToken * (i + 1),
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
                governanceToken * 200,
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
                governanceToken * 300,
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
                ShareFromGovernance(5 + 1 + 300),
                _states.GetBalance(delegationAddressB, Asset.Share));
            Assert.Equal(
                Asset.ConsensusFromGovernance(GovernanceToken * (1 + 5 + 1 + 300)),
                _states.GetBalance(ValidatorAddresses[5], Asset.ConsensusToken));
            Assert.Equal(
                governanceToken
                * (100 + (101 + 200) * 50 - 101 - 102 + 204 + 306),
                _states.GetBalance(ReservedAddress.BondedPool, governanceToken));
            Assert.Equal(
                governanceToken
                * (100 + (1 + 100) * 50 - 4 - 6 + 101 + 102),
                _states.GetBalance(ReservedAddress.UnbondedPool, governanceToken));
            Assert.Equal(
                governanceToken
                * (100000 - (1 + 200) * 100 - 200 - 300),
                _states.GetBalance(DelegatorAddress, governanceToken));

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
                ShareFromGovernance(0),
                _states.GetBalance(delegationAddressB, Asset.Share));
            Assert.Equal(
                Asset.ConsensusFromGovernance(GovernanceToken * 1),
                _states.GetBalance(validatorAddressB, Asset.ConsensusToken));
            Assert.Equal(
                governanceToken * (100 + (101 + 200) * 50 - 101 - 102 + 204 + 306 - 306),
                _states.GetBalance(ReservedAddress.BondedPool, governanceToken));
            Assert.Equal(
                governanceToken * (100 + (1 + 100) * 50 - 4 - 6 + 101 + 102 + 306),
                _states.GetBalance(ReservedAddress.UnbondedPool, governanceToken));
            Assert.Equal(
                governanceToken * (100000 - (1 + 200) * 100 - 200 - 300),
                _states.GetBalance(DelegatorAddress, governanceToken));

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
                governanceToken * (100 + (101 + 200) * 50 - 101 - 102 + 204 + 306 - 306 + 102),
                _states.GetBalance(ReservedAddress.BondedPool, governanceToken));
            Assert.Equal(
                governanceToken * (100 + (1 + 100) * 50 - 4 - 6 + 101 + 102 + 306 - 102),
                _states.GetBalance(ReservedAddress.UnbondedPool, governanceToken));
            Assert.Equal(
                governanceToken * (100000 - (1 + 200) * 100 - 200 - 300),
                _states.GetBalance(DelegatorAddress, governanceToken));

            _states = ValidatorSetCtrl.Update(
                _states,
                new ActionContext
                {
                    PreviousState = _states,
                    BlockIndex = 50400 * 5,
                });

            Assert.Equal(
                governanceToken * (100 + (1 + 100) * 50 - 4 - 6 + 101 + 102 + 306 - 102 - 306),
                _states.GetBalance(ReservedAddress.UnbondedPool, governanceToken));
            Assert.Equal(
                governanceToken * (100000 - (1 + 200) * 100 - 200 - 300 + 306),
                _states.GetBalance(DelegatorAddress, governanceToken));
        }
    }
}
