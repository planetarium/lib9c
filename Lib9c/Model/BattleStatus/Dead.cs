using System;
using System.Collections;
using Nekoyume.Model.Character;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class Dead : EventBase
    {
        public Dead(ICharacter character) : base(character)
        {
        }

        public override IEnumerator CoExecute(IWorld world)
        {
            yield return world.CoDead(Character);
        }
    }
}
