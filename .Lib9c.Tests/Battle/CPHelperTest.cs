namespace Lib9c.Tests
{
    using Lib9c.Tests.Action;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class CPHelperTest
    {
        private IAccountStateDelta _state;
        private TableSheets _tableSheets;

        public CPHelperTest()
        {
            _state = new Action.State();
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            foreach (var (key, value) in sheets)
            {
                _state = _state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);
            _state = _state.SetState(
                Addresses.GameConfig,
                new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(200)]
        [InlineData(300)]
        public void New_GetCP_Equals_Old_GetCP(int avatarLevel)
        {
            var avatar = new AvatarState(
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                0,
                _tableSheets.GetAvatarSheets(),
                _state.GetGameConfigState(),
                new PrivateKey().ToAddress(),
                "tester")
            {
                level = avatarLevel,
            };
            var equipments = Doomfist.GetAllParts(_tableSheets, avatarLevel);
            foreach (var equipment in equipments)
            {
                equipment.Equip();
                avatar.inventory.AddItem(equipment);
            }

            var costumes = Doomfist.GetAllCostumes(_tableSheets, avatarLevel);
            foreach (var costume in costumes)
            {
                costume.Equip();
                avatar.inventory.AddItem(costume);
            }

            var newCP = CPHelper.GetCP(
                avatar.characterId,
                avatarLevel,
                equipments,
                costumes,
                _tableSheets.CharacterSheet,
                _tableSheets.CostumeStatSheet);
            var oldCP = CPHelper.GetCP(
                avatar,
                _tableSheets.CharacterSheet,
                _tableSheets.CostumeStatSheet);
            Assert.Equal(oldCP, newCP);
        }
    }
}
