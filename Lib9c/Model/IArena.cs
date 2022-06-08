using System.Collections;
using System.Collections.Generic;
using Nekoyume.Model.Item;

namespace Nekoyume.Model
{
    public interface IArena : IWorld
    {
        IEnumerator CoSpawnArenaPlayer(ArenaCharacter character);
        IEnumerator CoArenaTurnEnd(int turnNumber);
    }
}
