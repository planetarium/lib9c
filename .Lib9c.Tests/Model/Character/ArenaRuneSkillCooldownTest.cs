namespace Lib9c.Tests.Model.Character
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.Action;
    using Nekoyume.Arena;
    using Nekoyume.Model;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class ArenaRuneSkillCooldownTest
    {
        private readonly TableSheets _tableSheets;
        private readonly AvatarState _avatar1;
        private readonly AvatarState _avatar2;

        public ArenaRuneSkillCooldownTest()
        {
            _tableSheets = new TableSheets(
                TableSheetsImporter.ImportSheets());
            _avatar1 = AvatarState.Create(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                default);
            _avatar2 = AvatarState.Create(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                default);
        }

        [Fact]
        public void RuneSkillCooldownMap_Uses_RuneOptionSheet_Value()
        {
            // Arrange: rune 10051 level 1 has cooldown 20 in
            // RuneOptionSheet, while SkillSheet has cooldown 15
            const int runeId = 10051;
            const int runeLevel = 1;
            const int expectedCooldown = 20;

            var runeStates =
                new List<RuneState> { new RuneState(runeId, runeLevel) };
            var allRuneState = new AllRuneState();
            allRuneState.AddRuneState(runeStates[0]);

            var runeSlotState = new RuneSlotState(BattleType.Arena);
            runeSlotState.UpdateSlot(
                new List<RuneSlotInfo>
                {
                    new RuneSlotInfo(3, runeId),
                },
                _tableSheets.RuneListSheet);

            var digest = new ArenaPlayerDigest(
                _avatar1,
                new List<Nekoyume.Model.Item.Costume>(),
                new List<Nekoyume.Model.Item.Equipment>(),
                allRuneState,
                runeSlotState);

            var simulator = new ArenaSimulator(new TestRandom());
            var arenaSheets =
                _tableSheets.GetArenaSimulatorSheets();

            var character = new Nekoyume.Model.ArenaCharacter(
                simulator,
                digest,
                arenaSheets,
                simulator.HpModifier,
                new List<StatModifier>());

            // Assert: RuneSkillCooldownMap should have the
            // RuneOptionSheet value (20), not SkillSheet value (15)
            Assert.True(
                character.RuneSkillCooldownMap.ContainsKey(700015));
            Assert.Equal(
                expectedCooldown,
                character.RuneSkillCooldownMap[700015]);
        }

        [Theory]
        [InlineData(1, 20)]
        [InlineData(2, 19)]
        [InlineData(3, 18)]
        public void RuneSkillCooldownMap_Varies_By_RuneLevel(
            int runeLevel,
            int expectedCooldown)
        {
            // Arrange: rune 10051 at different levels
            const int runeId = 10051;

            var allRuneState = new AllRuneState();
            allRuneState.AddRuneState(
                new RuneState(runeId, runeLevel));

            var runeSlotState = new RuneSlotState(BattleType.Arena);
            runeSlotState.UpdateSlot(
                new List<RuneSlotInfo>
                {
                    new RuneSlotInfo(3, runeId),
                },
                _tableSheets.RuneListSheet);

            var digest = new ArenaPlayerDigest(
                _avatar1,
                new List<Nekoyume.Model.Item.Costume>(),
                new List<Nekoyume.Model.Item.Equipment>(),
                allRuneState,
                runeSlotState);

            var simulator = new ArenaSimulator(new TestRandom());
            var character = new Nekoyume.Model.ArenaCharacter(
                simulator,
                digest,
                _tableSheets.GetArenaSimulatorSheets(),
                simulator.HpModifier,
                new List<StatModifier>());

            // Assert
            Assert.Equal(
                expectedCooldown,
                character.RuneSkillCooldownMap[700015]);
        }
    }
}
