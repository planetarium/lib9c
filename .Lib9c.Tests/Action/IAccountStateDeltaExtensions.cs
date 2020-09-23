namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;

    public static class IAccountStateDeltaExtensions
    {
        public static ImmutableDictionary<string, IValue> GetTotalDelta(
            this IAccountStateDelta accountStateDelta,
            Func<Address, string> toStateKey,
            Func<(Address, Currency), string> toFungibleAssetKey)
        {
            IImmutableSet<Address> stateUpdatedAddresses = accountStateDelta.UpdatedAddresses;
            IImmutableSet<(Address, Currency)> updatedFungibleAssets = accountStateDelta.UpdatedFungibleAssets
                .SelectMany(kv => kv.Value.Select(c => (kv.Key, c)))
                .ToImmutableHashSet();

            ImmutableDictionary<string, IValue> totalDelta =
                stateUpdatedAddresses.ToImmutableDictionary(
                    toStateKey,
                    a => accountStateDelta?.GetState(a)
                ).SetItems(
                    updatedFungibleAssets.Select(pair =>
                        new KeyValuePair<string, IValue>(
                            toFungibleAssetKey(pair),
                            new Bencodex.Types.Integer(
                                accountStateDelta?.GetBalance(pair.Item1, pair.Item2).RawValue ?? 0
                            )
                        )
                    )
                );

            return totalDelta;
        }
    }
}
