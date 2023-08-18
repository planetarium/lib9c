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
            context.UseGas(1);
            var world = context.PreviousState;
            if (context.Rehearsal)
            {
                return LegacyModule.SetState(world, Addresses.RedeemCode, MarkChanged);
            }

            CheckPermission(context);

            var redeem = LegacyModule.GetRedeemCodeState(world);
            var sheet = new RedeemCodeListSheet();
            sheet.Set(redeemCsv);
            redeem.Update(sheet);
            return LegacyModule.SetState(world, Addresses.RedeemCode, redeem.Serialize());
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
