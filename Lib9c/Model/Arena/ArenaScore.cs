using Bencodex.Types;
using Nekoyume.Model.State;
using Libplanet;
using Nekoyume.Action;

namespace Nekoyume.Model.Arena
{
    public class ArenaScore : IState
    {
        public static Address DeriveAddress(Address avatarAddress, int championshipId, int round) =>
            avatarAddress.Derive($"arena_score_{championshipId}_{round}");

        public Address Address;
        public int Score { get; private set; }

        public ArenaScore(Address avatarAddress, int championshipId, int round)
        {
            Address = DeriveAddress(avatarAddress, championshipId, round);
            Score = GameConfig.ArenaScoreDefault;
        }

        public ArenaScore(List serialized)
        {
            Address = serialized[0].ToAddress();
            Score = (Integer)serialized[1];
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(Address.Serialize())
                .Add(Score);
        }

        public void Update(int score)
        {
            Score = score;
        }
    }
}
