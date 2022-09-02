using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [ActionType("prepare_reward_assets")]
    public class PrepareRewardAssets : ActionBase
    {
        public Address RewardPoolAddress;
        public List<FungibleAssetValue> Assets;

        public PrepareRewardAssets(Address rewardPoolAddress, List<FungibleAssetValue> assets)
        {
            RewardPoolAddress = rewardPoolAddress;
            Assets = assets;
        }

        public PrepareRewardAssets()
        {
        }

        public override IValue PlainValue => Dictionary.Empty
            .Add("r", RewardPoolAddress.Serialize())
            .Add("a", Assets.Select(a => a.Serialize()));
        public override void LoadPlainValue(IValue plainValue)
        {
            var serialized = (Dictionary) plainValue;
            RewardPoolAddress = serialized["r"].ToAddress();
            Assets = serialized["a"].ToList(StateExtensions.ToFungibleAssetValue);
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IAccountStateDelta states = context.PreviousStates;
            if (context.Rehearsal)
            {
                foreach (var asset in Assets)
                {
                    return states.MarkBalanceChanged(asset.Currency, RewardPoolAddress);
                }
            }

            CheckPermission(context);

            foreach (var asset in Assets)
            {
                // Prevent mint NCG.
                if (!(asset.Currency.Minters is null))
                {
                    throw new CurrencyPermissionException(context.Signer, asset.Currency, null);
                }
                states = states.MintAsset(RewardPoolAddress, asset);
            }

            return states;
        }
    }
}
