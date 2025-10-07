using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.Item;
using Lib9c.Model.State;
using Lib9c.TableData.Quest;

namespace Lib9c.Model.Quest
{
    [Serializable]
    public class ItemGradeQuest : Quest
    {
        public readonly int Grade;
        public readonly List<int> ItemIds = new List<int>();
        public ItemGradeQuest(ItemGradeQuestSheet.Row data, QuestReward reward)
            : base(data, reward)
        {
            Grade = data.Grade;
        }

        public ItemGradeQuest(Dictionary serialized) : base(serialized)
        {
            Grade = serialized["grade"].ToInteger();
            ItemIds = serialized["itemIds"].ToList(i => i.ToInteger());
        }

        public ItemGradeQuest(List serialized) : base(serialized)
        {
            Grade = (Integer) serialized[7];
            ItemIds = serialized[8].ToList(i => (int)(Integer)i);
        }

        public override QuestType QuestType => QuestType.Obtain;

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

        public void Update(ItemUsable itemUsable)
        {
            if (Complete)
                return;

            if (!ItemIds.Contains(itemUsable.Id))
            {
                _current++;
                ItemIds.Add(itemUsable.Id);
                ItemIds.Sort();
            }
            Check();
        }

        protected override string TypeId => "itemGradeQuest";

        public override IValue Serialize() =>
            ((Dictionary)base.Serialize())
            .Add("grade", Grade.Serialize())
            .Add("itemIds", new List(ItemIds.OrderBy(i => i).Select(i => i.Serialize())));

        public override IValue SerializeList() =>
            ((List)base.SerializeList())
            .Add(Grade)
            .Add(new List(ItemIds.OrderBy(i => i)));
    }
}
