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
    public class ItemTypeCollectQuest : Quest
    {
        public readonly ItemType ItemType;
        public readonly List<int> ItemIds = new List<int>();

        public ItemTypeCollectQuest(ItemTypeCollectQuestSheet.Row data, QuestReward reward)
            : base(data, reward)
        {
            ItemType = data.ItemType;
        }

        public ItemTypeCollectQuest(Dictionary serialized) : base(serialized)
        {
            ItemIds = serialized["itemIds"].ToList(i => i.ToInteger());
            ItemType = serialized["itemType"].ToEnum<ItemType>();
        }

        public ItemTypeCollectQuest(List serialized) : base(serialized)
        {
            ItemIds = serialized[7].ToList(i => (int)(Integer)i);
            ItemType = serialized[8].ToEnum<ItemType>();
        }

        public void Update(ItemBase item)
        {
            if (Complete)
                return;

            if (!ItemIds.Contains(item.Id))
            {
                _current++;
                ItemIds.Add(item.Id);
                ItemIds.Sort();
            }

            Check();
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

        protected override string TypeId => "itemTypeCollectQuest";

        public override IValue Serialize() =>
            ((Dictionary) base.Serialize())
            .Add("itemType", ItemType.Serialize())
            .Add("itemIds", new List(ItemIds.OrderBy(i => i).Select(i => i.Serialize())));

        public override IValue SerializeList() =>
            ((List) base.SerializeList())
            .Add(new List(ItemIds.OrderBy(i => i)))
            .Add(ItemType.Serialize());
    }
}
