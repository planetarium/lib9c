using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Libplanet.Store.Trie;
using Libplanet.Types.Blocks;

namespace Libplanet.Extensions.RemoteStates;

public class RemoteWorldState : IWorldState
{
    private readonly Uri _explorerEndpoint;
    private readonly GraphQLHttpClient _graphQlHttpClient;
    private readonly BlockHash? _offset;

    public RemoteWorldState(Uri explorerEndpoint, BlockHash? offset)
    {
        _explorerEndpoint = explorerEndpoint;
        _offset = offset;
    }

    public IAccount GetAccount(Address address)
    {
        return new RemoteAccount(
            new RemoteAccountState(_explorerEndpoint, address, _offset));
    }

    public ITrie Trie => throw new NotSupportedException();

    public bool Legacy => throw new NotSupportedException();

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
