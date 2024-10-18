namespace Lib9c.Tests.Model.Skill.Raid
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Crypto;
    using Nekoyume.Battle;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class NormalAttackTest
    {
        private readonly TableSheets _tableSheets = new (TableSheetsImporter.ImportSheets());

        [Fact]
        public void FocusSkill()
        {
            const int seed = 10; // This seed fails to attack enemy with NormalAttack

            // With Focus buff
            var avatarState = AvatarState.Create(
                new PrivateKey().Address,
                new PrivateKey().Address,
                0,
                _tableSheets.GetAvatarSheets(),
                new PrivateKey().Address);
            avatarState.level = 400;

            var simulator = new RaidSimulator(
                _tableSheets.WorldBossListSheet.First().Value.BossId,
                new TestRandom(seed),
                avatarState,
                new List<Guid>(),
                new AllRuneState(),
                new RuneSlotState(BattleType.Raid),
                _tableSheets.GetRaidSimulatorSheets(),
                _tableSheets.CostumeStatSheet,
                new List<StatModifier>
                {
                    new (StatType.DEF, StatModifier.OperationType.Percentage, 100),
                },
                _tableSheets.DeBuffLimitSheet,
                _tableSheets.BuffLinkSheet
            );
            var player = simulator.Player;
            var buffRow = new ActionBuffSheet.Row();
            buffRow.Set(
                new List<string>
                    { "706000", "706000", "100", "9999", "Self", "Focus", "Normal", "0", }
            );
            player.AddBuff(new Focus(buffRow));

            var logs = simulator.Simulate();
            var playerLog = logs.Where(lg => lg.Character?.Id == player.Id);
            foreach (var log in playerLog)
            {
                if (log is NormalAttack or BlowAttack or DoubleAttack)
                {
                    Assert.True(((Skill)log).SkillInfos.First().Effect > 0);
                }
            }
        }
    }
}
