using System.Security.Cryptography;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Store.Trie;
using Libplanet.Types.Blocks;

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
}
