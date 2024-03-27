using System.Security.Cryptography;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Store.Trie;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;

namespace Libplanet.Extensions.RemoteBlockChainStates;

public class RemoteWorldState : IWorldState
{
    private readonly Uri _explorerEndpoint;
    private readonly GraphQLHttpClient _graphQlHttpClient;

    public RemoteWorldState(Uri explorerEndpoint, BlockHash? offsetBlockHash)
    {
        _explorerEndpoint = explorerEndpoint;
        _graphQlHttpClient = new GraphQLHttpClient(_explorerEndpoint, new SystemTextJsonSerializer());
        var response = _graphQlHttpClient.SendQueryAsync<GetWorldStateResponseType>(
            new GraphQLRequest(
                @"query GetWorld($address: Address!, $offsetBlockHash: ID!)
                {
                    stateQuery
                    {
                        worldState(offsetBlockHash: $offsetBlockHash)
                        {
                            string
                        }
                    }
                }",
                operationName: "GetWorld",
                variables: new
                {
                    offsetBlockHash = offsetBlockHash is { } hash
                        ? ByteUtil.Hex(hash.ByteArray)
                        : throw new NotSupportedException(),
                })).Result;

        Trie = new HollowTrie(HashDigest<SHA256>.FromString(response.Data.StateQuery.WorldState.StateRootHash));
        Legacy = response.Data.StateQuery.WorldState.Legacy;
    }

    public RemoteWorldState(Uri explorerEndpoint, HashDigest<SHA256>? offsetStateRootHash)
    {
        _explorerEndpoint = explorerEndpoint;
        _graphQlHttpClient = new GraphQLHttpClient(_explorerEndpoint, new SystemTextJsonSerializer());
        var response = _graphQlHttpClient.SendQueryAsync<GetWorldStateResponseType>(
            new GraphQLRequest(
                @"query GetWorld($address: Address!, $offsetStateRootHash: ID!)
                {
                    stateQuery
                    {
                        worldState(offsetStateRootHash: $offsetStateRootHash)
                        {
                            string
                        }
                    }
                }",
                operationName: "GetWorld",
                variables: new
                {
                    offsetStateRootHash = offsetStateRootHash is { } hash
                        ? ByteUtil.Hex(hash.ByteArray)
                        : throw new NotSupportedException(),
                })).Result;

        Trie = new HollowTrie(offsetStateRootHash);
        Legacy = response.Data.StateQuery.WorldState.Legacy;
    }
    public ITrie Trie { get; }

    public bool Legacy { get; private set; }

    public IAccountState GetAccountState(Address address) =>
        new RemoteAccountState(_explorerEndpoint, address, Trie.Hash);

    public FungibleAssetValue GetBalance(Address address, Currency currency)
    {
        object? currencyInput = currency.TotalSupplyTrackable ? new
        {
            ticker = currency.Ticker,
            decimalPlaces = currency.DecimalPlaces,
            minters = currency.Minters?.Select(addr => addr.ToString()).ToArray(),
            totalSupplyTrackable = currency.TotalSupplyTrackable,
            maximumSupplyMajorUnit = currency.MaximumSupply.Value.MajorUnit,
            maximumSupplyMinorUnit = currency.MaximumSupply.Value.MinorUnit,
        } : new
        {
            ticker = currency.Ticker,
            decimalPlaces = currency.DecimalPlaces,
            minters = currency.Minters?.Select(addr => addr.ToString()).ToArray(),
            totalSupplyTrackable = currency.TotalSupplyTrackable,
        };
        var response = _graphQlHttpClient.SendQueryAsync<GetBalanceResponseType>(
            new GraphQLRequest(
            @"query GetBalance(
                $owner: Address!,
                $currency: CurrencyInput!,
                $accountStateRootHash: HashDigest_SHA256!)
            {
                stateQuery
                {
                    balance(
                        owner: $owner,
                        currency: $currency,
                        accountStateRootHash: $accountStateRootHash)
                    {
                        string
                    }
                }
            }",
            operationName: "GetBalance",
            variables: new
            {
                owner = address.ToString(),
                currency = currencyInput,
                accountStateRootHash = Trie.Hash is { } accountSrh
                    ? ByteUtil.Hex(accountSrh.ByteArray)
                    : null,
            })).Result;

        return FungibleAssetValue.Parse(currency, response.Data.StateQuery.Balance.String.Split()[0]);
    }

    public FungibleAssetValue GetTotalSupply(Currency currency)
    {
        object? currencyInput = currency.TotalSupplyTrackable ? new
        {
            ticker = currency.Ticker,
            decimalPlaces = currency.DecimalPlaces,
            minters = currency.Minters.Select(addr => addr.ToString()).ToArray(),
            totalSupplyTrackable = currency.TotalSupplyTrackable,
            maximumSupplyMajorUnit = currency.MaximumSupply.Value.MajorUnit,
            maximumSupplyMinorUnit = currency.MaximumSupply.Value.MinorUnit,
        } : new
        {
            ticker = currency.Ticker,
            decimalPlaces = currency.DecimalPlaces,
            minters = currency.Minters.Select(addr => addr.ToString()).ToArray(),
            totalSupplyTrackable = currency.TotalSupplyTrackable,
        };
        var response = _graphQlHttpClient.SendQueryAsync<GetTotalSupplyResponseType>(
            new GraphQLRequest(
                @"query GetTotalSupply(
                    $currency: CurrencyInput!,
                    $accountStateRootHash: HashDigest_SHA256!)
                {
                    stateQuery
                    {
                        totalSupply(
                            currency: $currency,
                            offsetBlockHash: $offsetBlockHash
                            accountStateRootHash: $accountStateRootHash)
                        {
                            string
                        }
                    }
                }",
                operationName: "GetTotalSupply",
                variables: new
                {
                    currency = currencyInput,
                    accountStateRootHash = Trie.Hash is { } accountSrh
                        ? ByteUtil.Hex(accountSrh.ByteArray)
                        : null,
                })).Result;

        return FungibleAssetValue.Parse(currency, response.Data.StateQuery.TotalSupply.String.Split()[0]);
    }

    public ValidatorSet GetValidatorSet()
    {
        var response = _graphQlHttpClient.SendQueryAsync<GetValidatorsResponseType>(
            new GraphQLRequest(
                @"query GetValidators(
                    $accountStateRootHash: HashDigest_SHA256!)
                {
                    stateQuery
                    {
                        validators(
                            accountStateRootHash: $accountStateRootHash)
                        {
                            publicKey
                            power
                        }
                    }
                }",
                operationName: "GetValidators",
                variables: new
                {
                    accountStateRootHash = Trie.Hash is { } accountSrh
                        ? ByteUtil.Hex(accountSrh.ByteArray)
                        : null,
                })).Result;

        return new ValidatorSet(response.Data.StateQuery.Validators
            .Select(x =>
                new Validator(new PublicKey(ByteUtil.ParseHex(x.PublicKey)), x.Power))
            .ToList());
    }

    public class GetWorldStateResponseType
    {
        public StateQueryWithWorldStateType StateQuery { get; set; }
    }

    public class StateQueryWithWorldStateType
    {
        public WorldStateType WorldState { get; set; }
    }

    public class WorldStateType
    {
        public string StateRootHash { get; set; }

        public bool Legacy { get; set; }
    }

    private class GetBalanceResponseType
    {
        public StateQueryWithBalanceType StateQuery { get; set; }
    }

    private class StateQueryWithBalanceType
    {
        public FungibleAssetValueWithStringType Balance { get; set; }
    }

    private class FungibleAssetValueWithStringType
    {
        public string String { get; set; }
    }

    private class GetTotalSupplyResponseType
    {
        public StateQueryWithTotalSupplyType StateQuery { get; set; }
    }

    private class StateQueryWithTotalSupplyType
    {
        public FungibleAssetValueWithStringType TotalSupply { get; set; }
    }

    private class GetValidatorsResponseType
    {
        public StateQueryWithValidatorsType StateQuery { get; set; }
    }

    private class StateQueryWithValidatorsType
    {
        public ValidatorType[] Validators { get; set; }
    }

    private class ValidatorType
    {
        public string PublicKey { get; set; }

        public long Power { get; set; }
    }
}
