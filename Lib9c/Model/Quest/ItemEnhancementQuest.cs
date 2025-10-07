using System;
using System.Globalization;
using Bencodex.Types;
using Lib9c.Model.Item;
using Lib9c.TableData.Quest;

namespace Lib9c.Model.Quest
{
    [Serializable]
    public class ItemEnhancementQuest : Quest
    {
        public int Grade
        {
            get
            {
                if (_serializedGrade is { })
                {
                    _grade = (int) _serializedGrade;
                    _serializedGrade = null;
                }

                return _grade;
            }
        }

        public int Count
        {
            get
            {
                if (_serializedCount is { })
                {
                    _count = (int) _serializedCount;
                    _serializedCount = null;
                }

                return _count;
            }
        }

        private Integer? _serializedGrade;
        private int _grade;
        private Integer? _serializedCount;
        // Do not use this field. it can be different check result
        private int _count;
        public override float Progress => (float) _current / Count;

        public ItemEnhancementQuest(ItemEnhancementQuestSheet.Row data, QuestReward reward)
            : base(data, reward)
        {
            _count = data.Count;
            _grade = data.Grade;
        }

        public ItemEnhancementQuest(Dictionary serialized) : base(serialized)
        {
            _serializedGrade = (Integer) serialized["grade"];
            _serializedCount = (Integer) serialized["count"];
        }

        public ItemEnhancementQuest(List serialized) : base(serialized)
        {
            _serializedGrade = (Integer) serialized[7];
            _serializedCount = (Integer) serialized[8];
        }

        public override QuestType QuestType => QuestType.Craft;

        public override void Check()
        {
            if (Complete)
                return;

            Complete = Count == _current;
        }

        public override string GetProgressText() =>
            string.Format(
                CultureInfo.InvariantCulture,
                GoalFormat,
                Math.Min(Count, _current),
               Count
            );

        public void Update(Equipment equipment)
        {
            if (Complete)
                return;

            if (equipment.level == Goal)
            {
                _current++;
            }

            Check();
        }

        protected override string TypeId => "itemEnhancementQuest";

        public override IValue Serialize() =>
            ((Dictionary) base.Serialize())
            .Add("grade", _serializedGrade ?? Grade)
            .Add("count", _serializedCount ?? Count);

        public override IValue SerializeList() =>
            ((List) base.SerializeList())
            .Add(_serializedGrade ?? Grade)
            .Add(_serializedCount ?? Count);
    }
}
