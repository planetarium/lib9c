using System;
using System.Globalization;
using Bencodex.Types;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Quest
{
    public class CombinationEquipmentQuest : Quest
    {
        public int RecipeId
        {
            get
            {
                if (_serializedRecipeId is { })
                {
                    _recipeId = _serializedRecipeId switch
                    {
                        Integer i => i,
                        Text t => t.ToInteger(),
                        _ => throw new ArgumentOutOfRangeException(),
                    };

                    _serializedRecipeId = null;
                }

                return _recipeId;
            }
        }

        public int StageId {
            get
            {
                if (_serializedStageId is { })
                {
                    _stageId = _serializedStageId switch
                    {
                        Integer i => i,
                        Text t => t.ToInteger(),
                        _ => throw new ArgumentOutOfRangeException(),
                    };

                    _serializedStageId = null;
                }

                return _stageId;
            }
        }
        private IValue _serializedRecipeId;
        private int _recipeId;
        private IValue _serializedStageId;
        private int _stageId;

        public CombinationEquipmentQuest(QuestSheet.Row data, QuestReward reward, int stageId) : base(data, reward)
        {
            var row = (CombinationEquipmentQuestSheet.Row) data;
            _recipeId = row.RecipeId;
            _stageId = stageId;
        }

        public CombinationEquipmentQuest(Dictionary serialized) : base(serialized)
        {
            _serializedStageId = serialized["stage_id"];
            _serializedRecipeId = serialized["recipe_id"];
        }

        public CombinationEquipmentQuest(List serialized) : base(serialized)
        {
            _serializedRecipeId = serialized[7];
            _serializedStageId = serialized[8];
        }

        //임시처리. 새 타입을 만들어서 위젯에 띄워줘야합니다.
        public override QuestType QuestType => QuestType.Craft;

        public override void Check()
        {
            Complete = _current >= Goal;
        }

        protected override string TypeId => "combinationEquipmentQuest";

        public override string GetProgressText() =>
            string.Format(
                CultureInfo.InvariantCulture,
                GoalFormat,
                Math.Min(Goal, _current),
                Goal
            );

        public void Update(int recipeId)
        {
            if (Complete)
                return;

            if (recipeId == RecipeId)
            {
                _current++;
            }
            Check();
        }

        public override IValue Serialize()
        {
            return ((Dictionary) base.Serialize())
                .Add("recipe_id", _serializedRecipeId ?? RecipeId.Serialize())
                .Add("stage_id", _serializedStageId ?? StageId.Serialize());
        }

        public override IValue SerializeList()
        {
            return ((List) base.SerializeList())
                .Add(_serializedRecipeId ?? (Integer)RecipeId)
                .Add(_serializedStageId ?? (Integer)StageId);
        }
    }
}
