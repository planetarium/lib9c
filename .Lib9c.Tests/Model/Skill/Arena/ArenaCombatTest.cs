namespace Lib9c.Tests.Model.Skill.Arena
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.Arena;
    using Nekoyume.Model;
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.Skill.Arena;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Xunit;

    public class ArenaCombatTest
    {
        private readonly TableSheets _tableSheets;
        private readonly AvatarState _avatar1;
        private readonly AvatarState _avatar2;

        private readonly ArenaAvatarState _arenaAvatar1;
        private readonly ArenaAvatarState _arenaAvatar2;

        public ArenaCombatTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _avatar1 = AvatarState.Create(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );
            _avatar2 = AvatarState.Create(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                default
            );

            _arenaAvatar1 = new ArenaAvatarState(_avatar1);
            _arenaAvatar2 = new ArenaAvatarState(_avatar2);
        }

        [Theory]
        [InlineData(700009, new[] { 600001, })]
        [InlineData(700009, new[] { 600001, 704000, })]
        public void DispelOnUse(int dispelId, int[] debuffIdList)
        {
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var myDigest = new ArenaPlayerDigest(_avatar1, _arenaAvatar1);
            var enemyDigest = new ArenaPlayerDigest(_avatar2, _arenaAvatar2);
            var simulator = new ArenaSimulator(new TestRandom(1));
            var challenger = new ArenaCharacter(
                simulator,
                myDigest,
                arenaSheets,
                simulator.HpModifier,
                new List<StatModifier>()
            );
            var enemy = new ArenaCharacter(
                simulator,
                enemyDigest,
                arenaSheets,
                simulator.HpModifier,
                new List<StatModifier>()
            );

            // Add Debuff to avatar1
            foreach (var debuffId in debuffIdList)
            {
                var debuffRow = _tableSheets.ActionBuffSheet.Values.First(bf => bf.Id == debuffId);
                challenger.AddBuff(BuffFactory.GetActionBuff(challenger.Stats, debuffRow));
            }

            Assert.Equal(debuffIdList.Length, challenger.Buffs.Count);

            // Use Dispel
            var skillRow = _tableSheets.SkillSheet.Values.First(bf => bf.Id == dispelId);
            var dispelRow = _tableSheets.ActionBuffSheet.Values.First(
                bf => bf.Id == _tableSheets.SkillActionBuffSheet.OrderedList.First(
                        abf => abf.SkillId == dispelId)
                    .BuffIds.First()
            );
            var dispel = new ArenaBuffSkill(skillRow, 0, 100, 0, StatType.NONE);
            dispel.Use(
                challenger,
                challenger,
                simulator.Turn,
                BuffFactory.GetBuffs(
                    challenger.Stats,
                    dispel,
                    _tableSheets.SkillBuffSheet,
                    _tableSheets.StatBuffSheet,
                    _tableSheets.SkillActionBuffSheet,
                    _tableSheets.ActionBuffSheet
                ));
            Assert.Single(challenger.Buffs);
            Assert.Equal(dispelRow.GroupId, challenger.Buffs.First().Value.BuffInfo.GroupId);
        }

        [Fact]
        public void DispelOnDuration_Block()
        {
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var myDigest = new ArenaPlayerDigest(_avatar1, _arenaAvatar1);
            var enemyDigest = new ArenaPlayerDigest(_avatar2, _arenaAvatar2);
            var simulator = new ArenaSimulator(new TestRandom(1));
            var challenger = new ArenaCharacter(
                simulator,
                myDigest,
                arenaSheets,
                simulator.HpModifier,
                new List<StatModifier>()
            );
            var enemy = new ArenaCharacter(
                simulator,
                enemyDigest,
                arenaSheets,
                simulator.HpModifier,
                new List<StatModifier>()
            );

            // Use Dispel first
            var dispel = _tableSheets.ActionBuffSheet.Values.First(bf => bf.ActionBuffType == ActionBuffType.Dispel);
            challenger.AddBuff(BuffFactory.GetActionBuff(challenger.Stats, dispel));
            Assert.Single(challenger.Buffs);

            // Use Bleed
            var debuffRow =
                _tableSheets.SkillSheet.Values.First(bf => bf.Id == 600001); // 600001 is bleed
            var debuff = new ArenaBuffSkill(debuffRow, 100, 100, 0, StatType.NONE);
            var battleStatus = debuff.Use(
                enemy,
                challenger,
                simulator.Turn,
                BuffFactory.GetBuffs(
                    challenger.Stats,
                    debuff,
                    _tableSheets.SkillBuffSheet,
                    _tableSheets.StatBuffSheet,
                    _tableSheets.SkillActionBuffSheet,
                    _tableSheets.ActionBuffSheet
                )
            );
            Assert.Single(challenger.Buffs);
            Assert.False(battleStatus.SkillInfos.First().Affected);
        }

        [Fact]
        public void DispelOnDuration_Affect()
        {
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var myDigest = new ArenaPlayerDigest(_avatar1, _arenaAvatar1);
            var enemyDigest = new ArenaPlayerDigest(_avatar2, _arenaAvatar2);
            var simulator = new ArenaSimulator(new TestRandom());
            var challenger = new ArenaCharacter(
                simulator,
                myDigest,
                arenaSheets,
                simulator.HpModifier,
                new List<StatModifier>()
            );
            var enemy = new ArenaCharacter(
                simulator,
                enemyDigest,
                arenaSheets,
                simulator.HpModifier,
                new List<StatModifier>()
            );

            // Use Dispel first
            var dispel = _tableSheets.ActionBuffSheet.Values.First(bf => bf.ActionBuffType == ActionBuffType.Dispel);
            challenger.AddBuff(BuffFactory.GetActionBuff(challenger.Stats, dispel));
            Assert.Single(challenger.Buffs);

            // Use Focus
            var buffRow =
                _tableSheets.SkillSheet.Values.First(bf => bf.Id == 700007); // 700007 is Focus
            var buff = new ArenaBuffSkill(buffRow, 100, 100, 0, StatType.NONE);
            var battleStatus = buff.Use(
                challenger,
                challenger,
                simulator.Turn,
                BuffFactory.GetBuffs(
                    challenger.Stats,
                    buff,
                    _tableSheets.SkillBuffSheet,
                    _tableSheets.StatBuffSheet,
                    _tableSheets.SkillActionBuffSheet,
                    _tableSheets.ActionBuffSheet
                )
            );
            Assert.Equal(2, challenger.Buffs.Count);
            Assert.True(battleStatus.SkillInfos.First().Affected);
        }

        [Fact]
        public void DispelOnDuration_Nothing()
        {
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var myDigest = new ArenaPlayerDigest(_avatar1, _arenaAvatar1);
            var enemyDigest = new ArenaPlayerDigest(_avatar2, _arenaAvatar2);
            var simulator = new ArenaSimulator(new TestRandom());
            var challenger = new ArenaCharacter(
                simulator,
                myDigest,
                arenaSheets,
                simulator.HpModifier,
                new List<StatModifier>()
            );
            var enemy = new ArenaCharacter(
                simulator,
                enemyDigest,
                arenaSheets,
                simulator.HpModifier,
                new List<StatModifier>()
            );

            // Use Dispel first
            var dispel = _tableSheets.ActionBuffSheet.Values.First(bf => bf.ActionBuffType == ActionBuffType.Dispel);
            challenger.AddBuff(BuffFactory.GetActionBuff(challenger.Stats, dispel));
            Assert.Single(challenger.Buffs);

            // Use Bleed
            var bleed = _tableSheets.ActionBuffSheet.Values.First(bf => bf.Id == 600001);
            challenger.AddBuff(BuffFactory.GetActionBuff(challenger.Stats, bleed));
            Assert.Equal(2, challenger.Buffs.Count);

            // NormalAttack to test. This must not remove debuff by dispel
            var attackRow =
                _tableSheets.SkillSheet.Values.First(bf => bf.Id == 100000);
            var attack = new ArenaNormalAttack(attackRow, 100, 100, 0, StatType.NONE);

            // Hit
            var battleStatus = attack.Use(
                enemy,
                challenger,
                simulator.Turn,
                BuffFactory.GetBuffs(
                    challenger.Stats,
                    attack,
                    _tableSheets.SkillBuffSheet,
                    _tableSheets.StatBuffSheet,
                    _tableSheets.SkillActionBuffSheet,
                    _tableSheets.ActionBuffSheet
                )
            );
            Assert.Equal(2, challenger.Buffs.Count);
            Assert.True(battleStatus.SkillInfos.First().Affected);
            Assert.Contains(600001, challenger.Buffs.Values.Select(bf => bf.BuffInfo.Id));

            // Attack
            battleStatus = attack.Use(
                challenger,
                enemy,
                simulator.Turn,
                BuffFactory.GetBuffs(
                    challenger.Stats,
                    attack,
                    _tableSheets.SkillBuffSheet,
                    _tableSheets.StatBuffSheet,
                    _tableSheets.SkillActionBuffSheet,
                    _tableSheets.ActionBuffSheet
                )
            );
            Assert.Equal(2, challenger.Buffs.Count);
            Assert.True(battleStatus.SkillInfos.First().Affected);
            Assert.Contains(600001, challenger.Buffs.Values.Select(bf => bf.BuffInfo.Id));
        }

        [Fact]
        public void Drr()
        {
            var arenaSheets = _tableSheets.GetArenaSimulatorSheets();
            var myDigest = new ArenaPlayerDigest(_avatar1, _arenaAvatar1);
            var enemyDigest = new ArenaPlayerDigest(_avatar2, _arenaAvatar2);
            var simulator = new ArenaSimulator(new TestRandom());
            var challenger = new ArenaCharacter(
                simulator,
                myDigest,
                arenaSheets,
                simulator.HpModifier,
                new List<StatModifier>()
            );
            var enemy = new ArenaCharacter(
                simulator,
                enemyDigest,
                arenaSheets,
                simulator.HpModifier,
                new List<StatModifier>()
            );
            var statBuffRow = new Nekoyume.TableData.StatBuffSheet.Row();
            var csv = $"101000,101000,20,10,Self,DRR,Add,{int.MaxValue},true";
            statBuffRow.Set(csv.Split(","));
            var buff = new StatBuff(statBuffRow);
            enemy.AddBuff(buff);
            Assert.True(enemy.DRR > 1000000);
            var attackRow =
                _tableSheets.SkillSheet.Values.First(bf => bf.Id == 100000);
            var attack = new ArenaNormalAttack(attackRow, 100, 100, 0, StatType.NONE);
            var arenaSkill = attack.Use(
                challenger,
                enemy,
                simulator.Turn,
                BuffFactory.GetBuffs(
                    challenger.Stats,
                    attack,
                    _tableSheets.SkillBuffSheet,
                    _tableSheets.StatBuffSheet,
                    _tableSheets.SkillActionBuffSheet,
                    _tableSheets.ActionBuffSheet
                )
            );
            var info = arenaSkill.SkillInfos.Single();
            Assert.True(info.Effect > 1);
        }
    }
}
