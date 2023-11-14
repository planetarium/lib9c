using BenchmarkDotNet.Attributes;
using Lib9c.Tests;
using Lib9c.Tests.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.State;

namespace Lib9c.ActionBenchmarks;

[SimpleJob]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
[MarkdownExporter]
public class ClaimItemBenchmark
{
    private readonly IAccount _initialState;
    private readonly Address _signerAddress;
    private readonly TableSheets _tableSheets;
    private readonly IEnumerable<FungibleAssetValue> _fungibleAssetValues;

    private IAccount state;
    private List<(Address, IReadOnlyList<FungibleAssetValue>)> data;

    [Params(1, 10, 100, 500, 1000)] public int RECIPIENT_COUNT;

    [Params(1, 5, 10)] public int FAV_COUNT;

    public ClaimItemBenchmark()
    {
        _initialState = new MockStateDelta();

        var tablePath = Path.Combine(Path.GetFullPath("../../../.."), "Lib9c", "TableCSV");
        var sheets = TableSheetsImporter.ImportSheets(tablePath);
        foreach (var (key, value) in sheets)
        {
            _initialState = _initialState
                .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
        }

        _tableSheets = new TableSheets(sheets);
        var itemIds = _tableSheets.CostumeItemSheet.Values.Take(10).Select(x => x.Id).ToList();
        var currencies = itemIds.Select(id => Currency.Legacy($"Item_T_{id}", 0, minters: null))
            .ToList();
        _fungibleAssetValues = currencies.Select(currency => currency * 1);

        _signerAddress = new PrivateKey().ToAddress();

        var context = new ActionContext();
        _initialState = _initialState
            .MintAsset(context, _signerAddress, currencies[0] * 1000000)
            .MintAsset(context, _signerAddress, currencies[1] * 1000000)
            .MintAsset(context, _signerAddress, currencies[2] * 1000000)
            .MintAsset(context, _signerAddress, currencies[3] * 1000000)
            .MintAsset(context, _signerAddress, currencies[4] * 1000000)
            .MintAsset(context, _signerAddress, currencies[5] * 1000000)
            .MintAsset(context, _signerAddress, currencies[6] * 1000000)
            .MintAsset(context, _signerAddress, currencies[7] * 1000000)
            .MintAsset(context, _signerAddress, currencies[8] * 1000000)
            .MintAsset(context, _signerAddress, currencies[9] * 1000000);
    }

    [GlobalSetup]
    public void Setup()
    {
        var (generatedState, addresses) = Enumerable.Range(0, RECIPIENT_COUNT).Aggregate(
            (_initialState, new List<Address>()), (acc, _) =>
            {
                var (accState, addresses) = acc;

                accState = GenerateAvatar(accState, out var address);
                addresses.Add(address);

                return (accState, addresses);
            });
        state = generatedState;
        data = addresses.SelectMany(address =>
        {
            return Enumerable.Range(1, FAV_COUNT)
                .Select(index => (address,
                    _fungibleAssetValues.Take(index)
                        .ToList() as IReadOnlyList<FungibleAssetValue>));
        }).ToList();
    }

    [Benchmark]
    public void ClaimItem()
    {
        new ClaimItems(data).Execute(new ActionContext
        {
            PreviousState = state,
            Signer = _signerAddress,
            BlockIndex = 0,
            Random = new TestRandom(),
        });
    }

    private IAccount GenerateAvatar(IAccount state, out Address avatarAddress)
    {
        var address = new PrivateKey().ToAddress();
        var agentState = new AgentState(address);
        avatarAddress = address.Derive("avatar");
        var rankingMapAddress = new PrivateKey().ToAddress();
        var avatarState = new AvatarState(
            avatarAddress,
            address,
            0,
            _tableSheets.GetAvatarSheets(),
            new GameConfigState(),
            rankingMapAddress)
        {
            worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                GameConfig.RequireClearedStageLevel.ActionsInShop),
        };
        agentState.avatarAddresses[0] = avatarAddress;

        state = state
            .SetState(address, agentState.Serialize())
            .SetState(avatarAddress, avatarState.Serialize())
            .SetState(
                avatarAddress.Derive(SerializeKeys.LegacyInventoryKey),
                avatarState.inventory.Serialize());

        return state;
    }
}
