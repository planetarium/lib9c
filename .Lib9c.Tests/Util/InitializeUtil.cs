namespace Lib9c.Tests.Util
{
    using System.Collections.Generic;
    using System.IO;
    using Lib9c.Tests.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;

    public static class InitializeUtil
    {
        public static (
            TableSheets tableSheets,
            Address agentAddr,
            Address avatarAddr,
            IWorld initialStatesWithAvatarState
            ) InitializeStates(
                Address? adminAddr = null,
                Address? agentAddr = null,
                int avatarIndex = 0,
                bool isDevEx = false,
                Dictionary<string, string> sheetsOverride = null)
        {
            adminAddr ??= new PrivateKey().ToAddress();
            var context = new ActionContext();
            IWorld states = new MockWorld();
            states = LegacyModule.SetState(
                states,
                Addresses.Admin,
                new AdminState(adminAddr.Value, long.MaxValue).Serialize());

            var goldCurrency = Currency.Legacy(
                "NCG",
                2,
                minters: default
            );
            var goldCurrencyState = new GoldCurrencyState(goldCurrency);
            states = LegacyModule.SetState(
                states,
                goldCurrencyState.address,
                goldCurrencyState.Serialize());
            states = LegacyModule.MintAsset(
                states,
                context,
                goldCurrencyState.address,
                goldCurrency * 1_000_000_000);

            var world = new MockWorld(states);

            var tuple = InitializeTableSheets(world, isDevEx, sheetsOverride);
            states = tuple.world;
            var tableSheets = new TableSheets(tuple.sheets, ignoreFailedGetProperty: true);
            var gameConfigState = new GameConfigState(tuple.sheets[nameof(GameConfigSheet)]);
            states = LegacyModule.SetState(
                states,
                gameConfigState.address,
                gameConfigState.Serialize());

            agentAddr ??= new PrivateKey().ToAddress();
            var avatarAddr = Addresses.GetAvatarAddress(agentAddr.Value, avatarIndex);
            var agentState = new AgentState(agentAddr.Value);
            var avatarState = new AvatarState(
                avatarAddr,
                agentAddr.Value,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                avatarAddr.Derive("ranking_map"));
            agentState.avatarAddresses.Add(avatarIndex, avatarAddr);

            var initialStatesWithAvatarState = states;
            initialStatesWithAvatarState = AgentModule.SetAgentState(
                initialStatesWithAvatarState,
                agentAddr.Value,
                agentState);
            initialStatesWithAvatarState = AvatarModule.SetAvatarState(
                initialStatesWithAvatarState,
                avatarAddr,
                avatarState,
                true,
                true,
                true,
                true);

            return (
                tableSheets,
                agentAddr.Value,
                avatarAddr,
                new MockWorld(initialStatesWithAvatarState));
        }

        private static (IWorld world, Dictionary<string, string> sheets)
            InitializeTableSheets(
                IWorld world,
                bool isDevEx = false,
                Dictionary<string, string> sheetsOverride = null)
        {
            var sheets = TableSheetsImporter.ImportSheets(
                isDevEx
                    ? Path.GetFullPath("../../").Replace(
                        Path.Combine(".Lib9c.DevExtensions.Tests", "bin"),
                        Path.Combine("Lib9c", "TableCSV"))
                    : null
            );
            if (sheetsOverride != null)
            {
                foreach (var (key, value) in sheetsOverride)
                {
                    sheets[key] = value;
                }
            }

            foreach (var (key, value) in sheets)
            {
                world = world.SetAccount(
                    ReservedAddresses.LegacyAccount,
                    world.GetAccount(ReservedAddresses.LegacyAccount).SetState(
                        Addresses.TableSheet.Derive(key),
                        value.Serialize()));
            }

            return (world, sheets);
        }
    }
}
