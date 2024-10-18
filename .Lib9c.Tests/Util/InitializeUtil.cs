namespace Lib9c.Tests.Util
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Lib9c.Tests.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;

    public static class InitializeUtil
    {
        public static (
            TableSheets tableSheets,
            Address agentAddr,
            Address avatarAddr,
            IWorld world) InitializeStates(
                Address? adminAddr = null,
                Address? agentAddr = null,
                int avatarIndex = 0,
                bool isDevEx = false,
                Dictionary<string, string> sheetsOverride = null)
        {
            adminAddr ??= new PrivateKey().Address;
            var context = new ActionContext();
            var world = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(
                    Addresses.Admin,
                    new AdminState(adminAddr.Value, long.MaxValue).Serialize());

            var goldCurrency = Currency.Legacy(
                "NCG",
                2,
                minters: default
            );
            var goldCurrencyState = new GoldCurrencyState(goldCurrency);
            world = world
                .SetLegacyState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .MintAsset(context, goldCurrencyState.address, goldCurrency * 1_000_000_000);

            var tuple = InitializeTableSheets(world, isDevEx, sheetsOverride);
            world = tuple.states;
            var tableSheets = new TableSheets(tuple.sheets, true);
            var gameConfigState = new GameConfigState(tuple.sheets[nameof(GameConfigSheet)]);
            world = world.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            (world, var agentState) = AddAgent(world, agentAddr);
            (world, var avatarState, _) = AddAvatar(
                world,
                tableSheets.GetAvatarSheets(),
                agentState.address,
                avatarIndex);
            return (
                tableSheets,
                agentState.address,
                avatarState.address,
                world);
        }

        public static (IWorld world, TableSheets tableSheets) Initialize(
            Address? adminAddr = null,
            bool isDevEx = false,
            Dictionary<string, string> sheetsOverride = null)
        {
            adminAddr ??= new PrivateKey().Address;
            var world = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(
                    Addresses.Admin,
                    new AdminState(adminAddr.Value, long.MaxValue).Serialize());
            var goldCurrency = Currency.Legacy(
                "NCG",
                2,
                minters: default
            );
            var goldCurrencyState = new GoldCurrencyState(goldCurrency);
            var gold = goldCurrency * 1_000_000_000;
            world = world
                .SetLegacyState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .MintAsset(new ActionContext(), goldCurrencyState.address, gold);

            (world, var sheets) = InitializeTableSheets(world, isDevEx, sheetsOverride);
            var tableSheets = new TableSheets(sheets, true);
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            world = world.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());
            return (world, tableSheets);
        }

        public static (IWorld states, Dictionary<string, string> sheets)
            InitializeTableSheets(
                IWorld states,
                bool isDevEx = false,
                Dictionary<string, string> sheetsOverride = null)
        {
            var csvPath = isDevEx
                ? Path.GetFullPath("../../").Replace(
                    Path.Combine(".Lib9c.DevExtensions.Tests", "bin"),
                    Path.Combine("Lib9c", "TableCSV"))
                : null;
            var sheets = TableSheetsImporter.ImportSheets(csvPath);
            if (sheetsOverride != null)
            {
                foreach (var (key, value) in sheetsOverride)
                {
                    sheets[key] = value;
                }
            }

            foreach (var (key, value) in sheets)
            {
                var address = Addresses.TableSheet.Derive(key);
                states = states.SetLegacyState(address, value.Serialize());
            }

            return (states, sheets);
        }

        public static (IWorld world, AgentState agentState) AddAgent(
            IWorld world,
            Address? agentAddr = null)
        {
            agentAddr ??= new PrivateKey().Address;
            var agentState = new AgentState(agentAddr.Value);
            return (world.SetAgentState(agentAddr.Value, agentState), agentState);
        }

        public static (IWorld world, AvatarState avatarState, int index) AddAvatar(
            IWorld world,
            AvatarSheets avatarSheets,
            Address? agentAddr = null,
            int? index = 0,
            string name = null,
            int? level = 1,
            int? clearedStageId = 0)
        {
            agentAddr ??= new PrivateKey().Address;
            var agentState = world.GetAgentState(agentAddr.Value);
            if (agentState is null)
            {
                (world, agentState) = AddAgent(world, agentAddr.Value);
            }

            if (index is null)
            {
                for (var i = 0; i < GameConfig.SlotCount; i++)
                {
                    if (agentState.avatarAddresses.ContainsKey(i))
                    {
                        continue;
                    }

                    index = i;
                    break;
                }

                if (index is null)
                {
                    throw new InvalidOperationException("No available avatar slot.");
                }
            }
            else if (agentState.avatarAddresses.ContainsKey(index.Value))
            {
                throw new ArgumentException($"Avatar index {index.Value} is already in use.");
            }

            var avatarAddr = Addresses.GetAvatarAddress(agentAddr.Value, index.Value);
            var avatarState = AvatarState.Create(
                avatarAddr,
                agentAddr.Value,
                0,
                avatarSheets,
                avatarAddr.Derive("ranking_map"),
                name ?? $"Avatar_{index:D2}");
            avatarState.level = level ?? 1;
            if (clearedStageId is not null)
            {
                var worldSheet = world.GetSheet<WorldSheet>();
                avatarState.worldInformation = new WorldInformation(
                    0,
                    worldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInRankingBoard);
            }

            agentState.avatarAddresses.Add(index.Value, avatarAddr);
            return (world.SetAvatarState(avatarAddr, avatarState), avatarState, index.Value);
        }

        public static IWorld MainAsset(IWorld world, Address recipient, FungibleAssetValue asset)
        {
            return world.MintAsset(new ActionContext(), recipient, asset);
        }
    }
}
