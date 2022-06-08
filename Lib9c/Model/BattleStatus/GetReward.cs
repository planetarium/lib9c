using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Model.Item;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class GetReward : EventBase
    {
        public readonly List<ItemBase> Rewards;

        public GetReward(StageCharacter stageCharacter, List<ItemBase> rewards) : base(stageCharacter)
        {
            Rewards = rewards;
        }

        public override IEnumerator CoExecute(IWorld world)
        {
            if (world is IStage stage)
            {
                yield return stage.CoGetReward(Rewards);
            }
            else
            {
                throw new InvalidCastException(nameof(world));
            }
        }
    }
}
