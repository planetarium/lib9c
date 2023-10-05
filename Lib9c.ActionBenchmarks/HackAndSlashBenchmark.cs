using BenchmarkDotNet.Attributes;
using Bencodex.Types;
using Lib9c.Tests;
using Lib9c.Tests.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Lib9c.ActionBenchmarks;

[SimpleJob]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class HackAndSlashBenchmark
{
    private Dictionary<string, string> _sheets;
    private TableSheets _tableSheets;

    private Address _agentAddress;

    private Address _avatarAddress;
    private AvatarState _avatarState;

    private Address _inventoryAddress;
    private Address _worldInformationAddress;
    private Address _questListAddress;

    private Address _rankingMapAddress;

    private WeeklyArenaState _weeklyArenaState;
    private IAccount _initialState;
    private List<Guid> _costumes;
    private List<Equipment>? _equipments;

    const int worldId = 4;
    const int stageId = 200;
    const int avatarLevel = 200;

    [GlobalSetup]
    public void setup()
    {
        var tablePath = Path.Combine(Path.GetFullPath("../../../.."), "Lib9c", "TableCSV");
        _sheets = TableSheetsImporter.ImportSheets(tablePath);
        _tableSheets = new TableSheets(_sheets);

        var privateKey = new PrivateKey();
        _agentAddress = privateKey.PublicKey.ToAddress();
        var agentState = new AgentState(_agentAddress);

        _avatarAddress = _agentAddress.Derive("avatar");
        var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
        _rankingMapAddress = _avatarAddress.Derive("ranking_map");
        _avatarState = new AvatarState(
            _avatarAddress,
            _agentAddress,
            0,
            _tableSheets.GetAvatarSheets(),
            gameConfigState,
            _rankingMapAddress
        )
        {
            level = 100,
        };
        _inventoryAddress = _avatarAddress.Derive(LegacyInventoryKey);
        _worldInformationAddress = _avatarAddress.Derive(LegacyWorldInformationKey);
        _questListAddress = _avatarAddress.Derive(LegacyQuestListKey);
        agentState.avatarAddresses.Add(0, _avatarAddress);
        _weeklyArenaState = new WeeklyArenaState(0);
#pragma warning disable CS0618
        // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
        var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
        var goldCurrencyState = new GoldCurrencyState(currency);
        _initialState = new MockStateDelta()
            .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
            .SetState(_weeklyArenaState.address, _weeklyArenaState.Serialize())
            .SetState(_agentAddress, agentState.SerializeV2())
            .SetState(_avatarAddress, _avatarState.SerializeV2())
            .SetState(_inventoryAddress, _avatarState.inventory.Serialize())
            .SetState(_worldInformationAddress, _avatarState.worldInformation.Serialize())
            .SetState(_questListAddress, _avatarState.questList.Serialize())
            .SetState(gameConfigState.address, gameConfigState.Serialize());

        foreach (var (key, value) in _sheets)
        {
            _initialState = _initialState
                .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
        }

        foreach (var address in _avatarState.combinationSlotAddresses)
        {
            var slotState = new CombinationSlotState(
                address,
                GameConfig.RequireClearedStageLevel.CombinationEquipmentAction);
            _initialState = _initialState.SetState(address, slotState.Serialize());
        }

        var previousAvatarState = _initialState.GetAvatarStateV2(_avatarAddress);
        previousAvatarState.level = 200;
        var clearedStageId = _tableSheets.StageSheet.First?.Id ?? 0;
        clearedStageId = Math.Max(clearedStageId, stageId - 1);
        previousAvatarState.worldInformation = new WorldInformation(
            0,
            _tableSheets.WorldSheet,
            clearedStageId);

        _costumes = new List<Guid>();
        var random = new TestRandom();
        if (avatarLevel >= GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot)
        {
            var costumeId = _tableSheets
                .CostumeItemSheet
                .Values
                .First(r => r.ItemSubType == ItemSubType.FullCostume)
                .Id;

            var costume = (Costume)ItemFactory.CreateItem(
                _tableSheets.ItemSheet[costumeId], random);
            previousAvatarState.inventory.AddItem(costume);
            _costumes.Add(costume.ItemId);
        }

        _equipments = Doomfist.GetAllParts(_tableSheets, previousAvatarState.level);
        foreach (var equipment in _equipments)
        {
            var iLock = equipment.ItemSubType == ItemSubType.Weapon
                ? new OrderLock(Guid.NewGuid())
                : (ILock)null;
            previousAvatarState.inventory.AddItem(equipment, iLock: iLock);
        }

        var mailEquipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
        var mailEquipment = ItemFactory.CreateItemUsable(mailEquipmentRow, default, 0);
        var result = new CombinationConsumable5.ResultModel
        {
            id = default,
            gold = 0,
            actionPoint = 0,
            recipeId = 1,
            materials = new Dictionary<Material, int>(),
            itemUsable = mailEquipment,
        };
        for (var i = 0; i < 100; i++)
        {
            var mail = new CombinationMail(result, i, default, 0);
            previousAvatarState.Update(mail);
        }

        _initialState = _initialState
            .SetState(_avatarAddress, previousAvatarState.SerializeV2())
            .SetState(_avatarAddress.Derive(LegacyInventoryKey),
                previousAvatarState.inventory.Serialize())
            .SetState(_avatarAddress.Derive(LegacyWorldInformationKey),
                previousAvatarState.worldInformation.Serialize())
            .SetState(_avatarAddress.Derive(LegacyQuestListKey),
                previousAvatarState.questList.Serialize());

        _initialState = _initialState.SetState(
            _avatarAddress.Derive("world_ids"),
            List.Empty.Add(worldId.Serialize())
        );
    }

    [Benchmark]
    public void HackAndSlash()
    {
        var action = new HackAndSlash()
        {
            Costumes = _costumes,
            Equipments = _equipments.Select(e => e.NonFungibleId).ToList(),
            Foods = new List<Guid>(),
            RuneInfos = new List<RuneSlotInfo>(),
            WorldId = worldId,
            StageId = stageId,
            AvatarAddress = _avatarAddress,
        };
        action.Execute(new ActionContext
        {
            PreviousState = _initialState,
            Signer = _agentAddress,
            Random = new TestRandom(),
            Rehearsal = false,
            BlockIndex = ActionObsoleteConfig.V100301ExecutedBlockIndex,
        });
    }
}
