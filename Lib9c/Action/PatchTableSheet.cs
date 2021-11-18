using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using MessagePack;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("patch_table_sheet")]
    [MessagePackObject]
    public class PatchTableSheet : GameAction
    {
        [Key(1)]
        public string TableName;
        [Key(2)]
        public string TableCsv;

        public PatchTableSheet()
        {
        }

        [SerializationConstructor]
        public PatchTableSheet(Guid guid, string tableName, string tableCsv) : base(guid)
        {
            TableName = tableName;
            TableCsv = tableCsv;
        }

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

            CheckPermission(context);

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

        [IgnoreMember]
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
