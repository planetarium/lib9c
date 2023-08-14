using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.Extensions;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [ActionType("prepare_reward_assets")]
    public class PrepareRewardAssets : ActionBase, IPrepareRewardAssetsV1
    {
        public Address RewardPoolAddress;
        public List<FungibleAssetValue> Assets;

        Address IPrepareRewardAssetsV1.RewardPoolAddress => RewardPoolAddress;
        IEnumerable<FungibleAssetValue> IPrepareRewardAssetsV1.Assets => Assets;

        public PrepareRewardAssets(Address rewardPoolAddress, List<FungibleAssetValue> assets)
        {
            RewardPoolAddress = rewardPoolAddress;
            Assets = assets;
        }

        public PrepareRewardAssets()
        {
        }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", "prepare_reward_assets")
            .Add("values", Dictionary.Empty
                .Add("r", RewardPoolAddress.Serialize())
                .Add("a", new List(Assets.Select(a => a.Serialize()))));

        public override void LoadPlainValue(IValue plainValue)
        {
            var serialized = (Dictionary)((Dictionary)plainValue)["values"];
            RewardPoolAddress = serialized["r"].ToAddress();
            Assets = serialized["a"].ToList(StateExtensions.ToFungibleAssetValue);
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var world = context.PreviousState;
            var account = world.GetAccount(ReservedAddresses.LegacyAccount);
            if (context.Rehearsal)
            {
                // FIXME: Is this correct implementation?
                foreach (var asset in Assets)
                {
                    account = account.MarkBalanceChanged(context, asset.Currency, RewardPoolAddress);
                }

                return world.SetAccount(account);
            }

            CheckPermission(context);

            foreach (var asset in Assets)
            {
                // Prevent mint NCG.
                if (!(asset.Currency.Minters is null))
                {
                    throw new CurrencyPermissionException(null, context.Signer, asset.Currency);
                }
                account = account.MintAsset(context, RewardPoolAddress, asset);
            }

            return world.SetAccount(account);
        }
    }
}
