#nullable enable
namespace Lib9c.Tests.Action.ValidatorDelegation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Numerics;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Blocks;
    using Libplanet.Types.Consensus;
    using Libplanet.Types.Evidence;
    using Nekoyume;
    using Nekoyume.Action.ValidatorDelegation;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.ValidatorDelegation;

    public class ValidatorDelegationTestBase
    {
        protected static readonly Currency NCG = Currency.Uncapped("NCG", 2, null);

        public ValidatorDelegationTestBase()
        {
            var world = new World(MockUtil.MockModernWorldState);
            var goldCurrencyState = new GoldCurrencyState(NCG);
            World = world
                .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
        }

        protected static BlockHash EmptyBlockHash { get; }
            = new BlockHash(GetRandomArray(BlockHash.Size, _ => (byte)0x01));

        protected PrivateKey AdminKey { get; } = new PrivateKey();

        protected IWorld World { get; }

        protected static T[] GetRandomArray<T>(int length, Func<int, T> creator)
            => Enumerable.Range(0, length).Select(creator).ToArray();

        protected static IWorld MintAsset(
            IWorld world,
            PrivateKey delegatorPrivateKey,
            FungibleAssetValue amount,
            long blockHeight)
        {
            var actionContext = new ActionContext
            {
                PreviousState = world,
                BlockIndex = blockHeight,
            };
            return world.MintAsset(actionContext, delegatorPrivateKey.Address, amount);
        }

        protected static IWorld EnsureValidatorToBePromoted(
            IWorld world,
            PrivateKey validatorPrivateKey,
            FungibleAssetValue amount,
            long blockHeight)
        {
            var validatorPublicKey = validatorPrivateKey.PublicKey;
            var promoteValidator = new PromoteValidator(validatorPublicKey, amount);
            var actionContext = new ActionContext
            {
                PreviousState = MintAsset(
                    world, validatorPrivateKey, amount, blockHeight),
                Signer = validatorPublicKey.Address,
                BlockIndex = blockHeight,
            };
            return promoteValidator.Execute(actionContext);
        }
    }
}
