using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.State;
using Lib9c.TableData.Quest;
using Serilog;
using Boolean = Bencodex.Types.Boolean;

namespace Lib9c.Model.Quest
{
    public enum QuestType
    {
        Adventure,
        Obtain,
        Craft,
        Exchange
    }

    [Serializable]
    public abstract class Quest : IState
    {
        [NonSerialized]
        public bool isReceivable = false;

        protected int _current;

        public abstract QuestType QuestType { get; }

        private static readonly Dictionary<string, Func<Dictionary, Quest>> DictionaryDeserializers =
            new Dictionary<string, Func<Dictionary, Quest>>
            {
                ["collectQuest"] = d => new CollectQuest(d),
                ["combinationQuest"] = d => new CombinationQuest(d),
                ["monsterQuest"] = d => new MonsterQuest(d),
                ["tradeQuest"] = d => new TradeQuest(d),
                ["worldQuest"] = d => new WorldQuest(d),
                ["itemEnhancementQuest"] = d => new ItemEnhancementQuest(d),
                ["generalQuest"] = d => new GeneralQuest(d),
                ["itemGradeQuest"] = d => new ItemGradeQuest(d),
                ["itemTypeCollectQuest"] = d => new ItemTypeCollectQuest(d),
                ["GoldQuest"] = d => new GoldQuest(d),
                ["combinationEquipmentQuest"] = d => new CombinationEquipmentQuest(d),
            };

        private static readonly Dictionary<string, Func<List, Quest>> ListDeserializers =
            new Dictionary<string, Func<List, Quest>>
            {
                ["collectQuest"] = d => new CollectQuest(d),
                ["combinationQuest"] = d => new CombinationQuest(d),
                ["monsterQuest"] = d => new MonsterQuest(d),
                ["tradeQuest"] = d => new TradeQuest(d),
                ["worldQuest"] = d => new WorldQuest(d),
                ["itemEnhancementQuest"] = d => new ItemEnhancementQuest(d),
                ["generalQuest"] = d => new GeneralQuest(d),
                ["itemGradeQuest"] = d => new ItemGradeQuest(d),
                ["itemTypeCollectQuest"] = d => new ItemTypeCollectQuest(d),
                ["GoldQuest"] = d => new GoldQuest(d),
                ["combinationEquipmentQuest"] = d => new CombinationEquipmentQuest(d),
            };

        public bool Complete
        {
            get
            {
                if (_serializedComplete.HasValue)
                {
                    _complete = _serializedComplete.Value;
                    _serializedComplete = null;
                }

                return _complete;
            }
            protected set => _complete = value;
        }

        public int Goal {
            get
            {
                if (_serializedGoal.HasValue)
                {
                    _goal = _serializedGoal.Value;
                    _serializedGoal = null;
                }

                return _goal;
            }
        }

        public int Id
        {
            get
            {
                if (_serializedId.HasValue)
                {
                    _id = _serializedId.Value;
                    _serializedId = null;
                }

                return _id;
            }
        }

        public QuestReward Reward
        {
            get
            {
                if (_serializedReward is { })
                {
                    _reward = new QuestReward(_serializedReward);
                    _serializedReward = null;
                }

                return _reward;
            }
        }

        /// <summary>
        /// 이미 퀘스트 보상이 액션에서 지급되었는가?
        /// </summary>
        public bool IsPaidInAction { get; set; }

        public virtual float Progress => (float) _current / Goal;

        public const string GoalFormat = "({0}/{1})";
        private Dictionary _serializedReward;
        private QuestReward _reward;
        private Bencodex.Types.Boolean? _serializedComplete;
        private Integer? _serializedGoal;
        private int _goal;
        private Integer? _serializedId;
        private int _id;
        private bool _complete;

        protected Quest(QuestSheet.Row data, QuestReward reward)
        {
            _id = data.Id;
            _goal = data.Goal;
            _reward = reward;
        }

        public abstract void Check();
        protected abstract string TypeId { get; }

        protected Quest(Dictionary serialized)
        {
            _serializedComplete = (Bencodex.Types.Boolean) serialized["complete"];
            _serializedGoal = (Integer) serialized["goal"];
            _serializedId = (Integer) serialized["id"];
            _serializedReward = (Dictionary) serialized["reward"];
            IsPaidInAction = serialized["isPaidInAction"].ToNullableBoolean() ?? false;
            _current = (int) ((Integer) serialized["current"]).Value;
        }

        protected Quest(List serialized)
        {
            _serializedComplete = (Boolean) serialized[1];
            _serializedGoal = (Integer) serialized[2];
            _current = (Integer) serialized[3];
            _serializedId = (Integer) serialized[4];
            _serializedReward = (Dictionary) serialized[5];
            IsPaidInAction = (Boolean) serialized[6];
        }

        public abstract string GetProgressText();

        public virtual IValue Serialize() =>
            Dictionary.Empty
                .Add("typeId", (Text) TypeId)
                .Add("complete", _serializedComplete ?? new Bencodex.Types.Boolean(Complete))
                .Add("goal", _serializedGoal ?? Goal)
                .Add("current", (Integer) _current)
                .Add("id", _serializedId ?? Id)
                .Add("reward", _serializedReward ?? Reward.Serialize())
                .Add("isPaidInAction", new Bencodex.Types.Boolean(IsPaidInAction));

        public virtual IValue SerializeList() =>
            List.Empty
                .Add((Text)TypeId)
                .Add(_serializedComplete ?? new Boolean(Complete))
                .Add(_serializedGoal ?? Goal)
                .Add(_current)
                .Add(_serializedId ?? Id)
                .Add(_serializedReward ?? Reward.Serialize())
                .Add(new Boolean(IsPaidInAction));

        public static Quest Deserialize(Dictionary serialized)
        {
            string typeId = ((Text) serialized["typeId"]).Value;
            Func<Dictionary, Quest> deserializer;
            try
            {
                deserializer = DictionaryDeserializers[typeId];
            }
            catch (KeyNotFoundException)
            {
                string typeIds = string.Join(
                    ", ",
                    DictionaryDeserializers.Keys.OrderBy(k => k, StringComparer.InvariantCulture)
                );
                throw new ArgumentException(
                    $"Unregistered typeId: {typeId}; available typeIds: {typeIds}"
                );
            }

            try
            {
                return deserializer(serialized);
            }
            catch (Exception e)
            {
                Log.Error(
                    e,
                    "{TypeFullName} was raised during deserialize: {Serialized}",
                    e.GetType().FullName,
                    serialized);
                throw;
            }
        }

        public static Quest DeserializeList(List serialized)
        {
            string typeId = (Text) serialized[0];
            Func<List, Quest> deserializer;
            try
            {
                deserializer = ListDeserializers[typeId];
            }
            catch (KeyNotFoundException)
            {
                string typeIds = string.Join(
                    ", ",
                    ListDeserializers.Keys.OrderBy(k => k, StringComparer.InvariantCulture)
                );
                throw new ArgumentException(
                    $"Unregistered typeId: {typeId}; available typeIds: {typeIds}"
                );
            }

            try
            {
                return deserializer(serialized);
            }
            catch (Exception e)
            {
                Log.Error(
                    e,
                    "{TypeFullName} was raised during deserialize: {Serialized}",
                    e.GetType().FullName,
                    serialized);
                throw;
            }
        }

        public static Quest Deserialize(IValue arg)
        {
            if (arg is Dictionary d)
            {
                return Deserialize(d);
            }

            if (arg is List l)
            {
                return DeserializeList(l);
            }

            throw new ArgumentException();
        }
    }
}
