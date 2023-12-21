using Nekoyume.Model;
using System;

namespace Nekoyume.Battle
{
    public static class CriticalHelper
    {
        private const decimal MinimumDamageMultiplier = 1m;

        public static int GetCriticalDamage(CharacterBase caster, int originalDamage)
        {
            var critMultiplier =
                Math.Max(
                    MinimumDamageMultiplier,
                    CharacterBase.CriticalMultiplier + (caster.CDMG / 10000m));
            return (int)(originalDamage * critMultiplier);
        }

        public static int GetCriticalDamageForArena(ArenaCharacter caster, int originalDamage)
        {
            var critMultiplier =
                Math.Max(
                    MinimumDamageMultiplier,
                    ArenaCharacter.CriticalMultiplier + (caster.CDMG / 10000m));
            return (int)(originalDamage * critMultiplier);
        }
    }
}
