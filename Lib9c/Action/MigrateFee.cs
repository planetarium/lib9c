using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Model.State;
using Lib9c.Module;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.Action
{
    [ActionType(TypeIdentifier)]
    public class MigrateFee : ActionBase
    {
        public const string TypeIdentifier = "migrate_fee";
        public List<(Address sender, Address recipient, BigInteger amount)> TransferData;
        public string Memo;

        public MigrateFee()
        {
        }

        public override IValue PlainValue
        {
            get
            {
                var values = Dictionary.Empty
                    .Add("td",
                        new List(TransferData.Select(a =>
                            List.Empty
                                .Add(a.sender.Serialize())
                                .Add(a.recipient.Serialize())
                                .Add(a.amount.Serialize()))
                        )
                    );
                if (!string.IsNullOrEmpty(Memo))
                {
                    values = values.Add("m", Memo);
                }
                return Dictionary.Empty
                    .Add("type_id", TypeIdentifier)
                    .Add("values", values);
            }
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var dict = (Dictionary)((Dictionary)plainValue)["values"];
            var asList = (List) dict["td"];
            TransferData = new List<(Address sender, Address recipient, BigInteger amount)>();
            foreach (var v in asList)
            {
                var innerList = (List) v;
                var sender = innerList[0].ToAddress();
                var recipient = innerList[1].ToAddress();
                var amount = innerList[2].ToBigInteger();
                TransferData.Add((sender, recipient, amount));
            }

            if (dict.TryGetValue((Text)"m", out var m))
            {
                Memo = (Text) m;
            }
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            CheckPermission(context);
            var states = context.PreviousState;
            var goldCurrency = states.GetGoldCurrency();
            foreach (var (sender, recipient, raw) in TransferData)
            {
                var balance = states.GetBalance(sender, goldCurrency);
                var amount = FungibleAssetValue.FromRawValue(goldCurrency, raw);
                if (balance >= amount)
                {
                    states = states.TransferAsset(context, sender, recipient, amount);
                }
                else
                {
                    throw new InsufficientBalanceException(
                        $"required {amount} but {sender} balance is {balance}", sender, balance);
                }
            }

            return states;
        }
    }
}
