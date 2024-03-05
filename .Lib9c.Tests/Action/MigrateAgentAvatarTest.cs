namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Loader;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class MigrateAgentAvatarTest
    {
        private readonly TableSheets _tableSheets;

        public MigrateAgentAvatarTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            sheets[nameof(CharacterSheet)] = string.Join(
                Environment.NewLine,
                "id,_name,size_type,elemental_type,hp,atk,def,cri,hit,spd,lv_hp,lv_atk,lv_def,lv_cri,lv_hit,lv_spd,attack_range,run_speed",
                "100010,전사,S,0,300,20,10,10,90,70,12,0.8,0.4,0,3.6,2.8,2,3");
            _tableSheets = new TableSheets(sheets);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(2, true)]
        public void MigrateAgentAvatar(int legacyAvatarVersion, bool alreadyMigrated)
        {
            var avatarIndex = 1;
            var agentAddress = new PrivateKey().Address;
            var agentState = new AgentState(agentAddress);
            var avatarAddress = agentAddress.Derive(string.Format(CultureInfo.InvariantCulture, CreateAvatar.DeriveFormat, avatarIndex));
            agentState.avatarAddresses.Add(avatarIndex, avatarAddress);

            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);

            var weekly = new WeeklyArenaState(0);
            var gameConfigState = new GameConfigState();
            gameConfigState.Set(_tableSheets.GameConfigSheet);
            var currency = Currency.Legacy("NCG", 2, null);

            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                456,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default);

            MockWorldState mock = new MockWorldState()
                .SetState(
                    ReservedAddresses.LegacyAccount,
                    GoldCurrencyState.Address,
                    new GoldCurrencyState(currency, 0).Serialize())
                .SetState(
                    ReservedAddresses.LegacyAccount,
                    weekly.address,
                    weekly.Serialize())
                .SetState(
                    ReservedAddresses.LegacyAccount,
                    Addresses.GoldDistribution,
                    new List())
                .SetState(
                    ReservedAddresses.LegacyAccount,
                    gameConfigState.address,
                    gameConfigState.Serialize());

            switch (legacyAvatarVersion)
            {
                case 1:
                    mock = mock
                        .SetState(
                            ReservedAddresses.LegacyAccount,
                            agentAddress,
                            agentState.SerializeList())
                        .SetState(
                            ReservedAddresses.LegacyAccount,
                            avatarAddress,
                            MigrationAvatarState.LegacySerializeV1(avatarState));
                    break;
                case 2:
                    mock = mock
                        .SetState(
                            ReservedAddresses.LegacyAccount,
                            agentAddress,
                            agentState.SerializeList())
                        .SetState(
                            ReservedAddresses.LegacyAccount,
                            avatarAddress,
                            MigrationAvatarState.LegacySerializeV1(avatarState))
                        .SetState(
                            ReservedAddresses.LegacyAccount,
                            inventoryAddress,
                            avatarState.inventory.Serialize())
                        .SetState(
                            ReservedAddresses.LegacyAccount,
                            worldInformationAddress,
                            avatarState.questList.Serialize())
                        .SetState(
                            ReservedAddresses.LegacyAccount,
                            questListAddress,
                            avatarState.questList.Serialize());
                    break;
                default:
                    throw new ArgumentException($"Invalid legacy avatar version: {legacyAvatarVersion}");
            }

            if (alreadyMigrated)
            {
                mock = mock
                    .SetState(
                        Addresses.Agent,
                        agentAddress,
                        agentState.SerializeList())
                    .SetState(
                        Addresses.Avatar,
                        avatarAddress,
                        avatarState.SerializeList())
                    .SetState(
                        Addresses.Inventory,
                        avatarAddress,
                        avatarState.inventory.Serialize())
                    .SetState(
                        Addresses.WorldInformation,
                        avatarAddress,
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        Addresses.QuestList,
                        avatarAddress,
                        avatarState.questList.Serialize());
            }

            IAction action = new MigrateAgentAvatar
            {
                AgentAddresses = new List<Address> { agentAddress },
            };

            var plainValue = action.PlainValue;
            var actionLoader = new NCActionLoader();
            action = actionLoader.LoadAction(123, plainValue);

            var states = new World(mock);
            IWorld nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = states,
                    Miner = default,
                }
            );

            Assert.Null(nextState.GetLegacyState(agentAddress));
            Assert.Null(nextState.GetLegacyState(avatarAddress));
            Assert.Null(nextState.GetLegacyState(inventoryAddress));
            Assert.Null(nextState.GetLegacyState(worldInformationAddress));
            Assert.Null(nextState.GetLegacyState(questListAddress));

            Assert.NotNull(nextState.GetAccount(Addresses.Agent).GetState(agentAddress));
            Assert.NotNull(nextState.GetAccount(Addresses.Avatar).GetState(avatarAddress));
            Assert.NotNull(nextState.GetAccount(Addresses.Inventory).GetState(avatarAddress));
            Assert.NotNull(nextState.GetAccount(Addresses.WorldInformation).GetState(avatarAddress));
            Assert.NotNull(nextState.GetAccount(Addresses.QuestList).GetState(avatarAddress));
        }
    }
}
