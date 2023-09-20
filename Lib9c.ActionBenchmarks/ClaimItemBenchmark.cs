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

[SimpleJob(iterationCount: 5)]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class ClaimItemBenchmark
{
    private readonly IAccount _initialState;
    private readonly Address _signerAddress;
    private readonly Address _recipientAvatarAddress;

    private IEnumerable<FungibleAssetValue> _fungibleAssetValues;

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

        var tableSheets = new TableSheets(sheets);
        var itemIds = tableSheets.CostumeItemSheet.Values.Take(3).Select(x => x.Id).ToList();
        var currencies = itemIds.Select(id => Currency.Legacy($"it_{id}", 0, minters: null)).ToList();
        _fungibleAssetValues = currencies.Select(currency => currency * 1);

        _signerAddress = new PrivateKey().ToAddress();
        var recipientAddress = new PrivateKey().ToAddress();
        var recipientAgentState = new AgentState(recipientAddress);
        _recipientAvatarAddress = recipientAddress.Derive("avatar");
        var rankingMapAddress = new PrivateKey().ToAddress();
        var recipientAvatarState = new AvatarState(
            _recipientAvatarAddress,
            recipientAddress,
            0,
            tableSheets.GetAvatarSheets(),
            new GameConfigState(),
            rankingMapAddress)
        {
            worldInformation = new WorldInformation(
                0,
                tableSheets.WorldSheet,
                GameConfig.RequireClearedStageLevel.ActionsInShop),
        };
        recipientAgentState.avatarAddresses[0] = _recipientAvatarAddress;

        var context = new ActionContext();
        _initialState = _initialState
            .SetState(recipientAddress, recipientAgentState.Serialize())
            .SetState(_recipientAvatarAddress, recipientAvatarState.Serialize())
            .MintAsset(context, _signerAddress, currencies[0] * 1)
            .MintAsset(context, _signerAddress, currencies[1] * 1)
            .MintAsset(context, _signerAddress, currencies[2] * 1);
      }

    [Benchmark]
    public void ClaimItem()
    {
        var action = new ClaimItems(_recipientAvatarAddress, _fungibleAssetValues);
        action.Execute(new ActionContext
        {
            PreviousState = _initialState,
            Signer = _signerAddress,
            BlockIndex = 0,
            Random = new TestRandom(),
        });
    }
}
