using System;
using Bencodex.Types;
using CsvHelper;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.Model.State;
using Nekoyume.Module;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType(TypeIdentifier)]
    public class AdventureBossBattle : ActionBase
    {
        public const string TypeIdentifier = "adventure_boss_battle";
        public Address AvatarAddress;

        public override IValue PlainValue =>
            Dictionary.Empty
                .Add("type_id", TypeIdentifier)
                .Add("values", AvatarAddress.Serialize());
        public override void LoadPlainValue(IValue plainValue)
        {
            AvatarAddress = ((Dictionary)plainValue)["values"].ToAddress();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            var avatarState = states.GetAvatarState(AvatarAddress);
            if (avatarState.agentAddress != context.Signer)
            {
                throw new InvalidAddressException();
            }

            var season = context.BlockIndex / Wanted.SeasonInterval;
            AdventureInfo adventureInfo;
            try
            {
                adventureInfo = states.GetAdventureInfo(season, AvatarAddress);
            }
            catch (FailedLoadStateException)
            {
                adventureInfo = new AdventureInfo(AvatarAddress, 0, 0);
            }

            adventureInfo.Score += 100;
            adventureInfo.Floor++;
            return states.SetAdventureInfo(season, adventureInfo);
        }
    }
}
