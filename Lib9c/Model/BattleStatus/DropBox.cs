using System;
using System.Collections;
using System.Collections.Generic;
using Lib9c.Model.Character;
using Lib9c.Model.Item;

namespace Lib9c.Model.BattleStatus
{
    [Serializable]
    public class DropBox : EventBase
    {
        public readonly List<ItemBase> Items;

        public DropBox(CharacterBase character, List<ItemBase> items) : base(character)
        {
            Items = items;
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoDropBox(Items);
        }
    }
}
