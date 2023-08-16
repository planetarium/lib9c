using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Types.Blocks;

namespace Libplanet.Extensions.RemoteBlockChainStates;

public class RemoteWorldState : IWorldState
{
    private readonly Uri _explorerEndpoint;
    private readonly GraphQLHttpClient _graphQlHttpClient;

    public RemoteWorldState(Uri explorerEndpoint, BlockHash? offset)
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
                    }
                }",
                operationName: "GetWorld",
                variables: new
                {
                    offset = offset is { } hash
                        ? ByteUtil.Hex(hash.ByteArray)
                        : throw new NotSupportedException(),
                })).Result;
        BlockHash = Types.Blocks.BlockHash.FromString(response.Data.StateQuery.WorldState.BlockHash);
        Legacy = response.Data.StateQuery.WorldState.Legacy;
    }

    public IAccount GetAccount(Address address)
    {
        return new RemoteAccount(
            new RemoteAccountState(_explorerEndpoint, address, BlockHash));
    }

    public BlockHash? BlockHash { get; }

    public bool Legacy { get; private set; }

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
        public string BlockHash { get; set; }

        public bool Legacy { get; set; }
    }
}
