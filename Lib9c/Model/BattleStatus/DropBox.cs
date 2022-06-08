using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Model.Item;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class DropBox : EventBase
    {
        public readonly List<ItemBase> Items;

        public DropBox(StageCharacter stageCharacter, List<ItemBase> items) : base(stageCharacter)
        {
            Items = items;
        }

        public override IEnumerator CoExecute(IWorld world)
        {
            if (world is IStage stage)
            {
                yield return stage.CoDropBox(Items);
            }
            else
            {
                throw new InvalidCastException(nameof(world));
            }
        }
    }
}
