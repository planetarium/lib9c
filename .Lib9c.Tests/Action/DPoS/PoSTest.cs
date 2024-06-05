namespace Lib9c.Tests.Action.DPoS
{
    using System;
    using System.Collections.Immutable;
    using System.Numerics;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume.Action.DPoS.Control;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Model.State;
    using Nekoyume.Module;

    public class PoSTest
    {
        static PoSTest()
        {
            InitialState = new World(InitialStateHelper.EmptyWorldState.WithGoldCurrencyState());
            GovernanceToken = InitialState.GetGoldCurrency();
            NativeTokens = ImmutableHashSet.Create(
                GovernanceToken,
                Asset.ConsensusToken,
                Asset.Share);
        }

        /// <summary>
        /// An empty <see cref="IWorld"/> with its gold currency state set and its initial supply
        /// given to <see cref="GoldCurrencyState.Address"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="Currency"/> used for the gold currency state has a single randomized
        /// minter for each test.
        /// </remarks>
        protected static IWorld InitialState { get; }

        /// <summary>
        /// The set of native tokens including the hard coded ones in <see cref="Asset"/> and
        /// a randomized NCG type <see cref="Currency"/> set for <see cref="InitialState"/>.
        /// </summary>
        protected static ImmutableHashSet<Currency> NativeTokens { get; }

        /// <summary>
        /// The <see cref="Currency"/> used for governance.  This is consistent with
        /// the <see cref="Currency"/> of the gold currency state set for
        /// <see cref="InitialState"/>.
        /// </summary>
        protected static Currency GovernanceToken { get; }

        protected static IWorld InitializeStates()
        {
            return new World(MockWorldState.CreateModern());
        }

        protected static Address CreateAddress()
        {
            PrivateKey privateKey = new PrivateKey();
            return privateKey.Address;
        }

        protected static FungibleAssetValue ShareFromGovernance(FungibleAssetValue governanceToken)
            => FungibleAssetValue.FromRawValue(Asset.Share, governanceToken.RawValue);

        protected static FungibleAssetValue ShareFromGovernance(BigInteger amount)
            => ShareFromGovernance(Asset.GovernanceToken * amount);

        protected static IWorld Promote(
            IWorld states,
            long blockIndex,
            PublicKey operatorPublicKey,
            FungibleAssetValue ncg)
        {
            var operatorAddress = operatorPublicKey.Address;
            states = states.TransferAsset(
                context: new ActionContext(),
                sender: GoldCurrencyState.Address,
                recipient: operatorAddress,
                value: ncg);
            states = ValidatorCtrl.Create(
                states,
                new ActionContext { PreviousState = states, BlockIndex = blockIndex },
                operatorAddress,
                operatorPublicKey,
                ncg,
                NativeTokens);
            return states;
        }

        protected static IWorld Delegate(
            IWorld states,
            long blockIndex,
            Address validatorAddress,
            Address delegatorAddress,
            FungibleAssetValue ncg)
        {
            states = states.TransferAsset(
                context: new ActionContext(),
                sender: GoldCurrencyState.Address,
                recipient: delegatorAddress,
                value: ncg);
            states = DelegateCtrl.Execute(
                states: states,
                ctx: new ActionContext { PreviousState = states, BlockIndex = blockIndex },
                delegatorAddress: delegatorAddress,
                validatorAddress: validatorAddress,
                governanceToken: ncg,
                nativeTokens: NativeTokens);
            return states;
        }

        protected static IWorld DelegateMany(
            IWorld states,
            long blockIndex,
            Address validatorAddress,
            Address[] delegatorAddresses,
            FungibleAssetValue[] ncgs)
        {
            for (var i = 0; i < delegatorAddresses.Length; i++)
            {
                states = Delegate(
                    states: states,
                    blockIndex: blockIndex,
                    validatorAddress: validatorAddress,
                    delegatorAddress: delegatorAddresses[i],
                    ncg: ncgs[i]);
            }

            return states;
        }

        protected static IWorld Undelegate(
            IWorld states,
            long blockIndex,
            Address validatorAddress,
            Address delegatorAddress,
            FungibleAssetValue share)
        {
            states = UndelegateCtrl.Execute(
                states: states,
                ctx: new ActionContext { PreviousState = states, BlockIndex = blockIndex },
                delegatorAddress: delegatorAddress,
                validatorAddress: validatorAddress,
                share: share,
                nativeTokens: NativeTokens);
            return states;
        }

        protected static IWorld UndelegateMany(
            IWorld states,
            long blockIndex,
            Address validatorAddress,
            Address[] delegatorAddresses,
            FungibleAssetValue[] shares)
        {
            for (var i = 0; i < delegatorAddresses.Length; i++)
            {
                states = Undelegate(
                    states: states,
                    blockIndex: blockIndex,
                    validatorAddress: validatorAddress,
                    delegatorAddress: delegatorAddresses[i],
                    share: shares[i]);
            }

            return states;
        }

        protected static IWorld Redelegate(
            IWorld states,
            long blockIndex,
            Address srcValidatorAddress,
            Address dstValidatorAddress,
            Address delegatorAddress,
            FungibleAssetValue share)
        {
            states = RedelegateCtrl.Execute(
                states: states,
                ctx: new ActionContext { PreviousState = states, BlockIndex = blockIndex },
                delegatorAddress: delegatorAddress,
                srcValidatorAddress: srcValidatorAddress,
                dstValidatorAddress: dstValidatorAddress,
                redelegatingShare: share,
                nativeTokens: NativeTokens);
            return states;
        }

        protected static IWorld RedelegateMany(
            IWorld states,
            long blockIndex,
            Address srcValidatorAddress,
            Address dstValidatorAddress,
            Address[] delegatorAddresses,
            FungibleAssetValue[] shares)
        {
            for (var i = 0; i < delegatorAddresses.Length; i++)
            {
                states = Redelegate(
                    states: states,
                    blockIndex: blockIndex,
                    srcValidatorAddress: srcValidatorAddress,
                    dstValidatorAddress: dstValidatorAddress,
                    delegatorAddress: delegatorAddresses[i],
                    share: shares[i]);
            }

            return states;
        }

        protected static IWorld Update(
            IWorld states,
            long blockIndex)
        {
            states = ValidatorSetCtrl.Update(
                states: states,
                ctx: new ActionContext { PreviousState = states, BlockIndex = blockIndex, });
            return states;
        }

        protected static IWorld Jail(
            IWorld states,
            Address validatorAddress)
        {
            states = ValidatorCtrl.Jail(
                world: states,
                validatorAddress: validatorAddress);
            return states;
        }

        protected static IWorld JailIf(
            IWorld states,
            Address validatorAddress,
            bool condition)
        {
            if (condition == true)
            {
                states = ValidatorCtrl.Jail(
                world: states,
                validatorAddress: validatorAddress);
            }

            return states;
        }

        protected static IWorld JailUntil(
            IWorld states,
            Address validatorAddress,
            long blockHeight)
        {
            states = ValidatorCtrl.JailUntil(
                world: states,
                validatorAddress: validatorAddress,
                blockHeight: blockHeight);
            return states;
        }

        protected static IWorld Slash(
            IWorld states,
            long blockIndex,
            Address validatorAddress,
            long infractionHeight,
            BigInteger power,
            BigInteger slashFactor)
        {
            states = SlashCtrl.Slash(
                world: states,
                actionContext: new ActionContext { PreviousState = states, BlockIndex = blockIndex, },
                validatorAddress: validatorAddress,
                infractionHeight: infractionHeight,
                power: power,
                slashFactor: slashFactor,
                nativeTokens: NativeTokens);
            return states;
        }

        protected static IWorld Tombstone(
            IWorld states,
            Address validatorAddress)
        {
            states = ValidatorCtrl.Tombstone(
                world: states,
                validatorAddress: validatorAddress);
            return states;
        }

        protected sealed class BlockIndex : IDisposable
        {
            private readonly long _blockIndex;

            public BlockIndex(long blockIndex)
            {
                _blockIndex = blockIndex;
            }

            public static implicit operator long(BlockIndex blockIndex) => blockIndex._blockIndex;

            public void Dispose()
            {
            }
        }
    }
}
