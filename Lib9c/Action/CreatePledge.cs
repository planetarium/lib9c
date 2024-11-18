using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.Guild;
using Nekoyume.Model.State;
using Nekoyume.Module;

namespace Nekoyume.Action
{
    using Extensions;
    using Module.Guild;

    /// <summary>
    /// Admin action for pledge contract
    /// </summary>
    [ActionType(TypeIdentifier)]
    public class CreatePledge : ActionBase
    {
        public const string TypeIdentifier = "create_pledge";
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
            var serialized = (List)list[2];
            var agentAddresses = new List<(Address, Address)>();
            foreach (var value in serialized)
            {
                var innerList = (List)value;
                var agentAddress = innerList[0].ToAddress();
                var pledgeAddress = innerList[1].ToAddress();
                agentAddresses.Add((agentAddress, pledgeAddress));
            }

            AgentAddresses = agentAddresses;
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            CheckPermission(context);
            var states = context.PreviousState;
            var mead = Mead * Currencies.Mead;
            var contractList = List.Empty
                .Add(PatronAddress.Serialize())
                .Add(true.Serialize())
                .Add(Mead.Serialize());
            // migration for planetarium guild
            var repository = new GuildRepository(states, context);
            var planetariumGuildOwner = Nekoyume.Action.Guild.GuildConfig.PlanetariumGuildOwner;
            if (repository.GetJoinedGuild(planetariumGuildOwner) is null)
            {
                var random = context.GetRandom();
                var guildAddr = new Nekoyume.TypedAddress.GuildAddress(random.GenerateAddress());
                var guild = new Model.Guild.Guild(guildAddr, planetariumGuildOwner,
                    context.Miner, repository);
                repository.SetGuild(guild);
                repository = repository.JoinGuild(guildAddr, planetariumGuildOwner);
            }

            var guildAddress =
                (Nekoyume.TypedAddress.GuildAddress)
                repository.GetJoinedGuild(planetariumGuildOwner)!;
            foreach (var (agentAddress, pledgeAddress) in AgentAddresses)
            {
                if (PatronAddress == MeadConfig.PatronAddress)
                {
                    repository = repository.JoinGuild(guildAddress,
                        new Nekoyume.TypedAddress.AgentAddress(agentAddress));
                }

                repository.UpdateWorld(repository.World
                    .TransferAsset(context, PatronAddress, agentAddress, mead)
                    .SetLegacyState(pledgeAddress, contractList)
                );
            }

            return repository.World;
        }
    }
}
