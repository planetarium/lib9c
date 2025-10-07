using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Abstractions;
using Lib9c.Model.State;
using Lib9c.Module;
using Lib9c.TableData;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
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
    public class PatchTableSheet : GameAction, IPatchTableSheetV1
    {
        // FIXME: We should eliminate or justify this concept in another way after v100340.
        // (Until that) please consult Nine Chronicles Dev if you have any questions about this account.
        /// <summary>
        /// The operator address that has special permission to operation actions.
        /// When the action is signed by this operator, permission check is skipped.
        /// This is a temporary solution until v100340, after which this concept should be eliminated or justified differently.
        /// For any questions about this account, please consult Nine Chronicles Dev team.
        /// </summary>
        public static readonly Address Operator =
            new Address("3fe3106a3547488e157AED606587580e80375295");

        public string TableName;
        public string TableCsv;

        string IPatchTableSheetV1.TableName => TableName;
        string IPatchTableSheetV1.TableCsv => TableCsv;

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            IActionContext ctx = context;
            var states = ctx.PreviousState;
            var sheetAddress = Addresses.TableSheet.Derive(TableName);
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

            var sheet = states.GetLegacyState(sheetAddress);
            var value = sheet is null ? string.Empty : sheet.ToDotnetString();

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

            states = states.SetLegacyState(sheetAddress, TableCsv.Serialize());

            if (TableName == nameof(GameConfigSheet))
            {
                var gameConfigState = new GameConfigState(TableCsv);
                states = states.SetLegacyState(GameConfigState.Address, gameConfigState.Serialize());
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
