namespace Lib9c.Tests.Action.DPoS
{
    using System.Collections.Immutable;
    using System.Numerics;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action.DPoS.Control;
    using Nekoyume.Action.DPoS.Misc;
    using Nekoyume.Module;

    public class PoSTest
    {
        protected static readonly ImmutableHashSet<Currency> NativeTokens = ImmutableHashSet.Create(
            Asset.GovernanceToken, Asset.ConsensusToken, Asset.Share);

        protected static IWorld InitializeStates()
        {
            return new World(new MockWorldState());
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

        protected static IWorld Promote(IWorld states, long blockIndex, PublicKey operatorPublicKey, FungibleAssetValue governanceToken)
        {
            var operatorAddress = operatorPublicKey.Address;
            states = states.MintAsset(
                context: new ActionContext { PreviousState = states, BlockIndex = blockIndex },
                recipient: operatorAddress,
                value: governanceToken);
            states = ValidatorCtrl.Create(
                states,
                new ActionContext { PreviousState = states, BlockIndex = blockIndex },
                operatorAddress,
                operatorPublicKey,
                governanceToken,
                NativeTokens);
            return states;
        }

        protected static IWorld Delegate(
            IWorld states,
            long blockIndex,
            Address delegatorAddress,
            Address validatorAddress,
            FungibleAssetValue governanceToken)
        {
            states = states.MintAsset(
                context: new ActionContext { PreviousState = states, BlockIndex = blockIndex },
                recipient: delegatorAddress,
                value: governanceToken);
            states = DelegateCtrl.Execute(
                states: states,
                ctx: new ActionContext { PreviousState = states, BlockIndex = blockIndex },
                delegatorAddress: delegatorAddress,
                validatorAddress: validatorAddress,
                governanceToken: governanceToken,
                nativeTokens: NativeTokens);
            return states;
        }

        protected static IWorld Undelegate(
            IWorld states,
            long blockIndex,
            Address delegatorAddress,
            Address validatorAddress,
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

        protected static IWorld Redelegate(
            IWorld states,
            long blockIndex,
            Address delegatorAddress,
            Address srcValidatorAddress,
            Address dstValidatorAddress,
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
    }
}
