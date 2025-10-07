using System;
using System.Globalization;
using Bencodex.Types;
using Lib9c.TableData.Quest;

namespace Lib9c.Model.Quest
{
    [Serializable]
    public class MonsterQuest : Quest
    {
        public readonly int MonsterId;

        public MonsterQuest(MonsterQuestSheet.Row data, QuestReward reward)
            : base(data, reward)
        {
            MonsterId = data.MonsterId;
        }

        public MonsterQuest(Dictionary serialized) : base(serialized)
        {
            MonsterId = (int)((Integer)serialized["monsterId"]).Value;
        }

        public MonsterQuest(List serialized) : base(serialized)
        {
            MonsterId = (Integer)serialized[7];
        }

        public override QuestType QuestType => QuestType.Adventure;

        public override void Check()
        {
            if (Complete)
                return;

            Complete = _current >= Goal;
        }

        // FIXME: 이 메서드 구현은 중복된 코드가 다른 데서도 많이 있는 듯.
        public override string GetProgressText() =>
            string.Format(
                CultureInfo.InvariantCulture,
                GoalFormat,
                Math.Min(Goal, _current),
                Goal
            );

        protected override string TypeId => "monsterQuest";

        public void Update(CollectionMap monsterMap)
        {
            if (Complete)
                return;

            monsterMap.TryGetValue(MonsterId, out _current);
            Check();
        }

        public override IValue Serialize() =>
            ((Dictionary) base.Serialize())
            .Add("monsterId", MonsterId);

        public override IValue SerializeList() => ((List) base.SerializeList()).Add(MonsterId);
    }
}
