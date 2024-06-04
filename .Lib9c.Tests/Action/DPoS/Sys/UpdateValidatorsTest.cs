namespace Lib9c.Tests.Action.DPoS.Sys
{
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume.Action.DPoS;
    using Nekoyume.Action.DPoS.Control;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Action.DPoS.Model;
    using Nekoyume.Action.DPoS.Sys;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;

    public class UpdateValidatorsTest : PoSTest
    {
        [Fact]
        public void Execute()
        {
            // Prepare initial state.
            IWorld initialState = InitialState;
            var governanceToken = initialState.GetGoldCurrency();
            const int count = 4;
            var validatorKeys = Enumerable.Range(0, count).Select(_ => new PrivateKey().PublicKey).ToArray();
            initialState = validatorKeys.Aggregate(
                initialState,
                (current, key) => current.TransferAsset(
                    new ActionContext(),
                    GoldCurrencyState.Address,
                    key.Address,
                    new FungibleAssetValue(governanceToken, 1, 0)));
            foreach (var key in validatorKeys)
            {
                Assert.Equal(1, initialState.GetBalance(key.Address, governanceToken).MajorUnit);
                Assert.Equal(0, initialState.GetBalance(key.Address, governanceToken).MinorUnit);
            }

            // Stake 1 for each validator.
            foreach (var key in validatorKeys)
            {
                initialState = new PromoteValidator(
                    key,
                    new FungibleAssetValue(governanceToken, 1, 0)).Execute(
                        new ActionContext
                        {
                            PreviousState = initialState,
                            Signer = key.Address,
                        });
            }

            Assert.Equal(0, ValidatorSetCtrl.FetchBondedValidatorSet(initialState).Item2.Count);
            Assert.Equal(0, initialState.GetValidatorSet().TotalCount);

            // Execute the action.
            initialState = new UpdateValidators().Execute(
                new ActionContext
                {
                    PreviousState = initialState,
                    LastCommit = null,
                });

            Assert.Equal(count, ValidatorSetCtrl.FetchBondedValidatorSet(initialState).Item2.Count);
            Assert.Equal(count, initialState.GetValidatorSet().TotalCount);
            Assert.Equal(
                validatorKeys.ToHashSet(),
                initialState.GetValidatorSet()
                    .Validators.Select(validator => validator.PublicKey)
                    .ToHashSet());

            initialState = new Undelegate(
                Validator.DeriveAddress(validatorKeys[0].Address),
                new FungibleAssetValue(Asset.Share, 100, 0)).Execute(
                    new ActionContext
                    {
                        PreviousState = initialState,
                        Signer = validatorKeys[0].Address,
                    });

            Assert.Equal(count, ValidatorSetCtrl.FetchBondedValidatorSet(initialState).Item2.Count);
            Assert.Equal(count, initialState.GetValidatorSet().TotalCount);

            // Execute the action.
            initialState = new UpdateValidators().Execute(
                new ActionContext
                {
                    PreviousState = initialState,
                    LastCommit = null,
                });

            Assert.Equal(count - 1, ValidatorSetCtrl.FetchBondedValidatorSet(initialState).Item2.Count);
            Assert.Equal(count - 1, initialState.GetValidatorSet().TotalCount);
        }
    }
}
