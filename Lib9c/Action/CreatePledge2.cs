using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet;
using Libplanet.Action;
using Libplanet.State;
using Nekoyume.Model.State;
using Log = Serilog.Log;

namespace Nekoyume.Action
{
    [ActionType(TypeIdentifier)]
    public class CreatePledge2 : ActionBase
    {
        public const string TypeIdentifier = "create_pledge2";
        public Address PatronAddress;
        public int Mead;
        public IEnumerable<(Address, Address)> AgentAddresses;

        public override IValue PlainValue =>
            Dictionary.Empty
                .Add("type_id", TypeIdentifier)
                .Add("values", List.Empty
                    .Add(PatronAddress.Serialize())
                    .Add(Mead)
                    .Add(new List(AgentAddresses.Select(tuple =>
                        List.Empty
                            .Add(tuple.Item1.Serialize())
                            .Add(tuple.Item2.Serialize())
                    ))));
        public override void LoadPlainValue(IValue plainValue)
        {
            var list = (List)((Dictionary)plainValue)["values"];
            PatronAddress = list[0].ToAddress();
            Mead = (Integer)list[1];
            var serialized = (List) list[2];
            var agentAddresses = new List<(Address, Address)>();
            foreach (var value in serialized)
            {
                var innerList = (List) value;
                var agentAddress = innerList[0].ToAddress();
                var pledgeAddress = innerList[1].ToAddress();
                agentAddresses.Add((agentAddress, pledgeAddress));
            }
            AgentAddresses = agentAddresses;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var sw = new Stopwatch();
            sw.Start();
            context.UseGas(1);
            sw.Stop();
            Log.Debug("CreatePledge2 UseGas: {SwElapsed}", sw.Elapsed);
            sw.Restart();
            // Copy from CheckPermission
            var adminAddress = new Address("0xa1ef9701F151244F9aA7131639990c4664d2aEeF");
            if (context.BlockIndex > 8_000_000L)
            {
                throw new Exception();
            }
            if (adminAddress != context.Signer)
            {
                throw new Exception();
            }
            sw.Stop();
            Log.Debug("CreatePledge2 CheckPermission: {SwElapsed}", sw.Elapsed);
            sw.Restart();
            var states = context.PreviousStates;
            var mead = Mead * Currencies.Mead;
            var contractList = List.Empty
                .Add(PatronAddress.Serialize())
                .Add(true.Serialize())
                .Add(Mead.Serialize());
            foreach (var (agentAddress, pledgeAddress) in AgentAddresses)
            {
                states = states
                    .TransferAsset(PatronAddress, agentAddress, mead)
                    .SetState(pledgeAddress, contractList);
            }
            sw.Stop();
            Log.Debug(
                "CreatePledge2 Prepare Pledge({Count}): {SwElapsed}",
                AgentAddresses.Count(),
                sw.Elapsed);
            return states;
        }
    }
}
