using Nekoyume.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Nekoyume.Battle
{
    public static class CriticalHelper
    {
        private const decimal MinimumDamageMultiplier = 1m;

        public static long GetCriticalDamage(CharacterBase caster, long originalDamage)
        {
            var critMultiplier =
                Math.Max(
                    MinimumDamageMultiplier,
                    CharacterBase.CriticalMultiplier + (caster.CDMG / 10000m));
            return (long)(originalDamage * critMultiplier);
        }

        public static long GetCriticalDamageForArena(ArenaCharacter caster, long originalDamage)
        {
            var critMultiplier =
                Math.Max(
                    MinimumDamageMultiplier,
                    ArenaCharacter.CriticalMultiplier + (caster.CDMG / 10000m));
            return (long)(originalDamage * critMultiplier);
        }
    }
}
