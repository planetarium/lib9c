namespace Lib9c.Tests.Model.Arena
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Model.Character;
    using Lib9c.Model.EnumType;
    using Lib9c.Model.Item;
    using Lib9c.Model.Stat;
    using Lib9c.Model.State;
    using Lib9c.Tests.Action;
    using Libplanet.Crypto;
    using Xunit;

    public class PlayerDigestTest
    {
        private readonly AvatarState _avatarState;
        private readonly ArenaAvatarState _arenaAvatarState;
        private readonly TableSheets _tableSheets;

        public PlayerDigestTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var avatarState = AvatarState.Create(
                new PrivateKey().Address,
                new PrivateKey().Address,
                1234,
                _tableSheets.GetAvatarSheets(),
                new PrivateKey().Address,
                "test"
            );
            avatarState.hair = 2;
            avatarState.lens = 3;
            avatarState.ear = 4;
            avatarState.tail = 5;

            var costumeRow = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.ATK);
            var costume = (Costume)ItemFactory.CreateItem(_tableSheets.ItemSheet[costumeRow.CostumeId], new TestRandom(1));
            costume.equipped = true;
            avatarState.inventory.AddItem(costume);

            var costume2Row = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.DEF);
            var costume2 = (Costume)ItemFactory.CreateItem(_tableSheets.ItemSheet[costume2Row.CostumeId], new TestRandom(2));
            avatarState.inventory.AddItem(costume2);

            var weaponRow = _tableSheets.EquipmentItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Weapon);
            var weapon = (Weapon)ItemFactory.CreateItem(_tableSheets.ItemSheet[weaponRow.Id], new TestRandom(3));
            weapon.equipped = true;
            avatarState.inventory.AddItem(weapon);

            var armorRow =
                _tableSheets.EquipmentItemSheet.Values.First(
                    r => r.ItemSubType == ItemSubType.Armor);
            var armor = (Armor)ItemFactory.CreateItem(_tableSheets.ItemSheet[armorRow.Id], new TestRandom(4));
            avatarState.inventory.AddItem(armor);
            _avatarState = avatarState;

            _arenaAvatarState = new ArenaAvatarState(_avatarState);
            _arenaAvatarState.UpdateEquipment(new List<Guid>() { weapon.ItemId, armor.ItemId, });
            _arenaAvatarState.UpdateCostumes(new List<Guid>() { costume.ItemId, costume2.ItemId, });
        }

        [Fact]
        public void Constructor()
        {
            var digest = new ArenaPlayerDigest(_avatarState, _arenaAvatarState);

            Assert.Equal(_avatarState.NameWithHash, digest.NameWithHash);
            Assert.Equal(_avatarState.characterId, digest.CharacterId);
            Assert.Equal(2, digest.HairIndex);
            Assert.Equal(3, digest.LensIndex);
            Assert.Equal(4, digest.EarIndex);
            Assert.Equal(5, digest.TailIndex);
            Assert.Equal(2, digest.Equipments.Count);
            Assert.Equal(2, digest.Costumes.Count);

            var enemyPlayer = new EnemyPlayer(digest, _tableSheets.GetArenaSimulatorSheetsV1());
            var player = new Player(digest, _tableSheets.GetArenaSimulatorSheetsV1());

            Assert.Equal(2, enemyPlayer.Equipments.Count);
            Assert.Equal(2, enemyPlayer.Costumes.Count);
            Assert.Equal(2, player.Costumes.Count);
            Assert.Equal(2, player.Costumes.Count);
            Assert.Equal(enemyPlayer.Equipments.FirstOrDefault(), player.Equipments.FirstOrDefault());
            Assert.Equal(enemyPlayer.Costumes.FirstOrDefault(), player.Costumes.FirstOrDefault());
        }

        [Fact]
        public void SerializeWithoutRune()
        {
            var digest = new ArenaPlayerDigest(_avatarState, _arenaAvatarState);
            var serialized = digest.Serialize();
            var deserialized = new ArenaPlayerDigest((List)serialized);

            Assert.Equal(serialized, deserialized.Serialize());
        }

        [Theory]
        [InlineData(new int[] { })]
        [InlineData(new[] { 10001, })]
        [InlineData(new[] { 10001, 10002, })]
        public void SerializeWithRune(int[] runeIds)
        {
            var runes = new AllRuneState();
            foreach (var runeId in runeIds)
            {
                runes.AddRuneState(runeId);
            }

            var digest = new ArenaPlayerDigest(
                _avatarState,
                _arenaAvatarState.Equipments,
                _arenaAvatarState.Costumes,
                runes,
                new RuneSlotState(BattleType.Arena)
            );
            var serialized = digest.Serialize();
            var deserialized = new ArenaPlayerDigest((List)serialized);

            Assert.Equal(serialized, deserialized.Serialize());
        }
    }
}
