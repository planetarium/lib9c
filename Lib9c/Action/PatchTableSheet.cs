using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Model.State;
using Lib9c.TableData;
using Libplanet;
using Libplanet.Action;
using Serilog;

namespace Lib9c.Action
{
    /// <summary>
    /// Introduced at Initial commit(2e645be18a4e2caea031c347f00777fbad5dbcc6)
    /// Updated at https://github.com/planetarium/lib9c/pull/42
    /// Updated at https://github.com/planetarium/lib9c/pull/101
    /// Updated at https://github.com/planetarium/lib9c/pull/287
    /// Updated at https://github.com/planetarium/lib9c/pull/315
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// Updated at https://github.com/planetarium/lib9c/pull/1560
    /// </summary>
    [Serializable]
    [ActionType("patch_table_sheet")]
    public class PatchTableSheet : GameAction
    {
        // FIXME: We should eliminate or justify this concept in another way after v100340.
        // (Until that) please consult Nine Chronicles Dev if you have any questions about this account.
        private static readonly Address Operator =
            new Address("3fe3106a3547488e157AED606587580e80375295");

        public string TableName;
        public string TableCsv;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            var sheetAddress = Addresses.TableSheet.Derive(TableName);
            if (ctx.Rehearsal)
            {
                return states
                    .SetState(sheetAddress, MarkChanged)
                    .SetState(GameConfigState.Address, MarkChanged);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context);

#if !LIB9C_DEV_EXTENSIONS && !UNITY_EDITOR
            if (ctx.Signer == Operator)
            {
                Log.Information(
                    "Skip CheckPermission since {TxId} had been signed by the operator({Operator}).",
                    context.TxId,
                    Operator
                );
            }
            else
            {
                CheckPermission(context);
            }
#endif

            var sheets = states.GetState(sheetAddress);
            var value = sheets is null ? string.Empty : sheets.ToDotnetString();

            Log.Verbose(
                "{AddressesHex}{TableName} was patched\n" +
                "before:\n" +
                "{Value}\n" +
                "after:\n" +
                "{TableCsv}",
                addressesHex,
                TableName,
                value,
                TableCsv
            );

            states = states.SetState(sheetAddress, TableCsv.Serialize());

            if (TableName == nameof(GameConfigSheet))
            {
                var gameConfigState = new GameConfigState(TableCsv);
                states = states.SetState(GameConfigState.Address, gameConfigState.Serialize());
            }

            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .SetItem("table_name", (Text) TableName)
                .SetItem("table_csv", (Text) TableCsv);

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            TableName = (Text) plainValue["table_name"];
            TableCsv = (Text) plainValue["table_csv"];
        }
    }
}
