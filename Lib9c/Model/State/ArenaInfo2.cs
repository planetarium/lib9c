using System;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class ArenaInfo2 : IState
    {
        public readonly Address AvatarAddress;
        public readonly Address AgentAddress;
        public readonly string AvatarName;
        public readonly ArenaRecord ArenaRecord;
        public int Level { get; private set; }
        public int CombatPoint { get; private set; }
        public int ArmorId { get; private set; }
        public bool Active { get; private set; }
        public int DailyChallengeCount { get; private set; }
        public int Score { get; private set; }

        public ArenaInfo2(AvatarState avatarState, CharacterSheet characterSheet, CostumeStatSheet costumeStatSheet, bool active)
        {
            AvatarAddress = avatarState.address;
            AgentAddress = avatarState.agentAddress;
            AvatarName = avatarState.NameWithHash;
            ArenaRecord = new ArenaRecord();
            Level = avatarState.level;
            var armor = avatarState.inventory.Items.Select(i => i.item).OfType<Armor>().FirstOrDefault(e => e.equipped);
            ArmorId = armor?.Id ?? GameConfig.DefaultAvatarArmorId;
            CombatPoint = CPHelper.GetCP(avatarState, characterSheet);
            Active = active;
            DailyChallengeCount = GameConfig.ArenaChallengeCountMax;
            Score = GameConfig.ArenaScoreDefault;
            CombatPoint = CPHelper.GetCPV2(avatarState, characterSheet, costumeStatSheet);
        }

        public ArenaInfo2(List serialized)
        {
            AvatarAddress = serialized[0].ToAddress();
            AgentAddress = serialized[1].ToAddress();
            AvatarName = serialized[2].ToDotnetString();
            ArenaRecord = new ArenaRecord((List)serialized[3]);
            Level = serialized[4].ToInteger();
            ArmorId = serialized[5].ToInteger();
            CombatPoint = serialized[6].ToInteger();
            Active = serialized[7].ToBoolean();
            DailyChallengeCount = serialized[8].ToInteger();
            Score = serialized[9].ToInteger();
        }

        public ArenaInfo2(ArenaInfo prevInfo)
        {
            AvatarAddress = prevInfo.AvatarAddress;
            AgentAddress = prevInfo.AgentAddress;
            ArmorId = prevInfo.ArmorId;
            Level = prevInfo.Level;
            AvatarName = prevInfo.AvatarName;
            CombatPoint = prevInfo.CombatPoint;
            Score = prevInfo.Score;
            DailyChallengeCount = GameConfig.ArenaChallengeCountMax;
            Active = prevInfo.Active;
            ArenaRecord = new ArenaRecord
            {
                Win = prevInfo.ArenaRecord.Win,
                Lose = prevInfo.ArenaRecord.Lose,
                Draw = prevInfo.ArenaRecord.Draw,
            };
        }

        public IValue Serialize() =>
            List.Empty
                .Add(AvatarAddress.Serialize())
                .Add(AgentAddress.Serialize())
                .Add(AvatarName.Serialize())
                .Add(ArenaRecord.Serialize())
                .Add(Level.Serialize())
                .Add(ArmorId.Serialize())
                .Add(CombatPoint.Serialize())
                .Add(Active.Serialize())
                .Add(DailyChallengeCount.Serialize())
                .Add(Score.Serialize());

        public void Update(AvatarState state, CharacterSheet characterSheet)
        {
            ArmorId = state.GetArmorId();
            Level = state.level;
            CombatPoint = CPHelper.GetCP(state, characterSheet);
        }

        public void Update(AvatarState state, CharacterSheet characterSheet, CostumeStatSheet costumeStatSheet)
        {
            ArmorId = state.GetArmorId();
            Level = state.level;
            CombatPoint = CPHelper.GetCPV2(state, characterSheet, costumeStatSheet);
        }
        public int Update(AvatarState avatarState, ArenaInfo2 enemyInfo, BattleLog.Result result)
        {
            switch (result)
            {
                case BattleLog.Result.Win:
                    ArenaRecord.Win++;
                    break;
                case BattleLog.Result.Lose:
                    ArenaRecord.Lose++;
                    break;
                case BattleLog.Result.TimeOver:
                    ArenaRecord.Draw++;
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }

            var score = ArenaScoreHelper.GetScore(Score, enemyInfo.Score, result);
            var calculated = Score + score;
            var current = Score;
            Score = Math.Max(1000, calculated);
            DailyChallengeCount--;
            ArmorId = avatarState.GetArmorId();
            Level = avatarState.level;
            return Score - current;
        }

        public void Activate()
        {
            Active = true;
        }

        public void ResetCount()
        {
            DailyChallengeCount = GameConfig.ArenaChallengeCountMax;
        }

        public int GetRewardCount()
        {
            if (Score >= 1800)
            {
                return 6;
            }

            if (Score >= 1400)
            {
                return 5;
            }

            if (Score >= 1200)
            {
                return 4;
            }

            if (Score >= 1100)
            {
                return 3;
            }

            if (Score >= 1001)
            {
                return 2;
            }

            return 1;
        }

        protected bool Equals(ArenaInfo2 other)
        {
            return AvatarAddress.Equals(other.AvatarAddress) && AgentAddress.Equals(other.AgentAddress) &&
                   AvatarName == other.AvatarName && Equals(ArenaRecord, other.ArenaRecord) && Level == other.Level &&
                   CombatPoint == other.CombatPoint && ArmorId == other.ArmorId && Active == other.Active &&
                   DailyChallengeCount == other.DailyChallengeCount && Score == other.Score;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ArenaInfo2)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = AvatarAddress.GetHashCode();
                hashCode = (hashCode * 397) ^ AgentAddress.GetHashCode();
                hashCode = (hashCode * 397) ^ (AvatarName != null ? AvatarName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ArenaRecord != null ? ArenaRecord.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Level;
                hashCode = (hashCode * 397) ^ CombatPoint;
                hashCode = (hashCode * 397) ^ ArmorId;
                hashCode = (hashCode * 397) ^ Active.GetHashCode();
                hashCode = (hashCode * 397) ^ DailyChallengeCount;
                hashCode = (hashCode * 397) ^ Score;
                return hashCode;
            }
        }
    }
}
