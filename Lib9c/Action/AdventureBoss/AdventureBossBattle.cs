using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action.Exceptions.AdventureBoss;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.Model.State;
using Nekoyume.Module;

namespace Nekoyume.Action.AdventureBoss
{
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class AdventureBossBattle : ActionBase
    {
        public const string TypeIdentifier = "adventure_boss_battle";
        public int Season;
        public Address AvatarAddress;

        public override IValue PlainValue =>
            Dictionary.Empty
                .Add("type_id", TypeIdentifier)
                .Add("values", List.Empty
                    .Add(Season.Serialize())
                    .Add(AvatarAddress.Serialize())
                );

        public override void LoadPlainValue(IValue plainValue)
        {
            var values = (List)((Dictionary)plainValue)["values"];
            Season = values[0].ToInteger();
            AvatarAddress = values[1].ToAddress();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            var latestSeason = states.GetLatestAdventureBossSeason();
            if (latestSeason.Season != Season)
            {
                throw new InvalidAdventureBossSeasonException(
                    $"Given season {Season} is not current season: {latestSeason.Season}"
                );
            }

            var avatarState = states.GetAvatarState(AvatarAddress);
            if (avatarState.agentAddress != context.Signer)
            {
                throw new InvalidAddressException();
            }

            var exploreBoard = states.GetExploreBoard(Season);
            exploreBoard.AddExplorer(AvatarAddress);

            // TODO: Add used resources to currentSeason
            // TODO: AdventureBossSimulator with pass-through log

            Explorer explorer;
            try
            {
                explorer = states.GetExplorer(Season, AvatarAddress);
            }
            catch (FailedLoadStateException)
            {
                explorer = new Explorer(AvatarAddress, 0, 0);
            }

            explorer.Score += 100;
            explorer.Floor++;
            states = states.SetExploreBoard(Season, exploreBoard);
            return states.SetExploreInfo(Season, explorer);
        }
    }
}
