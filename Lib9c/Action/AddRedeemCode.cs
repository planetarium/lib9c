using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using MessagePack;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("add_redeem_code")]
    [MessagePackObject]
    public class AddRedeemCode : GameAction
    {
        [Key(1)]
        public string redeemCsv;

        public AddRedeemCode()
        {
        }

        [SerializationConstructor]
        public AddRedeemCode(Guid guid, string csv) : base(guid)
        {
            redeemCsv = csv;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states
                    .SetState(Addresses.RedeemCode, MarkChanged);
            }

            CheckPermission(context);

            var redeem = states.GetRedeemCodeState();
            var sheet = new RedeemCodeListSheet();
            sheet.Set(redeemCsv);
            redeem.Update(sheet);
            return states
                .SetState(Addresses.RedeemCode, redeem.Serialize());
        }

        [IgnoreMember]
        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .SetItem("redeem_csv", redeemCsv.Serialize());
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            redeemCsv = plainValue["redeem_csv"].ToDotnetString();
        }
    }
}
