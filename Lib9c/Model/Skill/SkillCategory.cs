namespace Lib9c.Model.Skill
{
    public enum SkillCategory
    {
        NormalAttack = 0,
        BlowAttack = 1,
        DoubleAttack = 2, // Attack enemy two times
        AreaAttack = 3,
        BuffRemovalAttack = 4,
        ShatterStrike = 5,  // Damage based on enemy's full HP

        Heal = 6,

        // todo: 코드상에서 버프와 디버프를 버프로 함께 구분하고 있는데, 고도화 될 수록 디버프를 구분해주게 될 것으로 보임.
        HPBuff = 7,
        AttackBuff = 8,
        DefenseBuff = 9,
        CriticalBuff = 10,
        HitBuff = 11,
        SpeedBuff = 12,
        DamageReductionBuff = 13,
        CriticalDamageBuff = 14,
        Buff = 15,
        Debuff = 16,
        TickDamage = 17,
        Focus = 18,  // Always hit enemy.
        Dispel = 19, // Remove/defence debuffs
    }
}
