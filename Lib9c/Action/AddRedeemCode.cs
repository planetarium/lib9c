using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("add_redeem_code")]
    public class AddRedeemCode : GameAction, IAddRedeemCodeV1
    {
        public string redeemCsv;

        string IAddRedeemCodeV1.RedeemCsv => redeemCsv;

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var states = context.PreviousState;

            CheckPermission(context);

            var redeem = states.GetRedeemCodeState();
            var sheet = new RedeemCodeListSheet();
            sheet.Set(redeemCsv);
            redeem.Update(sheet);
            return states
                .SetLegacyState(Addresses.RedeemCode, redeem.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .SetItem("redeem_csv", redeemCsv.Serialize());
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            redeemCsv = plainValue["redeem_csv"].ToDotnetString();
        }
    }
}
