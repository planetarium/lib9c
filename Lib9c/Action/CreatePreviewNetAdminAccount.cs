using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    /// <summary>
    // https://github.com/planetarium/lib9c/pull/1028
    /// </summary>
    [Serializable]
    [ActionObsolete(BlockChain.Policy.BlockPolicySource.PreviewNetAdminAccountAllowIndex)]
    [ActionType("create_preview_net_admin_account")]
    public class CreatePreviewNetAdminAccount : ActionBase
    {
        public override IValue PlainValue =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
            });

        public override void LoadPlainValue(IValue plainValue)
        {
        }

        public CreatePreviewNetAdminAccount()
        {
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;

            CheckObsolete(BlockChain.Policy.BlockPolicySource.PreviewNetAdminAccountAllowIndex, context);

            Currency goldCurrency = states.GetGoldCurrency();
            Address fund = GoldCurrencyState.Address;

            // 1 User max 10,000ncg, max 10,000 user = 100,000,000 ncg
            FungibleAssetValue fav = goldCurrency * 100_000_000;

            states = states.TransferAsset(
                fund,
                BlockChain.Policy.BlockPolicySource.PreviewNetAdmin,
                fav
            );

            return states;
        }
    }
}
