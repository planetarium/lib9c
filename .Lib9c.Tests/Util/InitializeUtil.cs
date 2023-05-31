#nullable enable

namespace Lib9c.Tests.Util
{
    using System.Collections.Immutable;
    using System.IO;
    using Libplanet;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Libplanet.State;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using State = Lib9c.Tests.Action.State;

    public static class InitializeUtil
    {
        public static (
            TableSheets tableSheets,
            Address agentAddress,
            Address avatarAddress,
            IAccountStateDelta initialStatesWithAvatarStateV1,
            IAccountStateDelta initialStatesWithAvatarStateV2
            ) InitializeStates(
                Address? adminAddr = null,
                Address? agentAddr = null,
                int avatarIndex = 0,
                bool isDevEx = false
            )
        {
            adminAddr ??= new PrivateKey().ToAddress();
            var states = new State().SetState(
                Addresses.Admin,
                new AdminState(adminAddr.Value, long.MaxValue).Serialize());
            var sheets = TableSheetsImporter.ImportSheets(
                isDevEx
                    ? Path.GetFullPath("../../").Replace(
                        Path.Combine(".Lib9c.DevExtensions.Tests", "bin"),
                        Path.Combine("Lib9c", "TableCSV"))
                    : null
            );
            foreach (var (key, value) in sheets)
            {
                states = states.SetState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            var tableSheets = new TableSheets(sheets);
            var goldCurrency = Currency.Legacy(
                "NCG",
                2,
                minters: new[] { adminAddr.Value }.ToImmutableHashSet());
            var goldCurrencyState = new GoldCurrencyState(goldCurrency);
            states = states
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .MintAsset(goldCurrencyState.address, goldCurrency * 1_000_000_000);

            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            states = states.SetState(gameConfigState.address, gameConfigState.Serialize());

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

            var initialStatesWithAvatarStateV1 = states
                .SetState(agentAddr.Value, agentState.Serialize())
                .SetState(avatarAddr, avatarState.Serialize());
            var initialStatesWithAvatarStateV2 = states
                .SetState(agentAddr.Value, agentState.Serialize())
                .SetState(avatarAddr, avatarState.SerializeV2())
                .SetState(
                    avatarAddr.Derive(SerializeKeys.LegacyInventoryKey),
                    avatarState.inventory.Serialize())
                .SetState(
                    avatarAddr.Derive(SerializeKeys.LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize())
                .SetState(
                    avatarAddr.Derive(SerializeKeys.LegacyQuestListKey),
                    avatarState.questList.Serialize());

            return (
                tableSheets,
                agentAddr.Value,
                avatarAddr,
                initialStatesWithAvatarStateV1,
                initialStatesWithAvatarStateV2);
        }
    }
}
