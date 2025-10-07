using System;
using System.Collections;
using System.Collections.Generic;
using Lib9c.Model.Character;
using Lib9c.Model.Item;

namespace Lib9c.Model.BattleStatus
{
    [Serializable]
    public class GetReward : EventBase
    {
        public readonly List<ItemBase> Rewards;

        public GetReward(CharacterBase character, List<ItemBase> rewards) : base(character)
        {
            Rewards = rewards;
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoGetReward(Rewards);
        }
    }
}
