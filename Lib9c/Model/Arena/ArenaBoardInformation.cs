using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Arena
{
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/1137
    /// </summary>
    public class ArenaBoardInformation : IState
    {
        public static Address DeriveAddress(Address avatarAddress, int championshipId, int round) =>
            avatarAddress.Derive($"arena_board_information_{championshipId}_{round}");

        public Address Address;

        public string NameWithHash { get; private set; }
        public int PortraitId { get; private set; }
        public int Level { get; private set; }
        public int CP { get; private set; }

        public ArenaBoardInformation(Address avatarAddress, int championshipId, int round)
        {
            Address = DeriveAddress(avatarAddress, championshipId, round);
            NameWithHash = string.Empty;
        }

        public ArenaBoardInformation(List serialized)
        {
            Address = serialized[0].ToAddress();
            NameWithHash = serialized[1].ToDotnetString();
            PortraitId = (Integer)serialized[2];
            Level = (Integer)serialized[3];
            CP = (Integer)serialized[4];
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(Address.Serialize())
                .Add(NameWithHash)
                .Add(PortraitId)
                .Add(Level)
                .Add(CP);
        }

        public void Update(string nameWithHash, int portraitId, int level, int cp)
        {
            NameWithHash = nameWithHash;
            PortraitId = portraitId;
            Level = level;
            CP = cp;
        }
    }
}
