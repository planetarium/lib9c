namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
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
                default);

            IWorld mock = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(
                    GoldCurrencyState.Address,
                    new GoldCurrencyState(currency, 0).Serialize())
                .SetLegacyState(
                    weekly.address,
                    weekly.Serialize())
                .SetLegacyState(
                    Addresses.GoldDistribution,
                    new List())
                .SetLegacyState(
                    gameConfigState.address,
                    gameConfigState.Serialize());

            switch (legacyAvatarVersion)
            {
                case 1:
                    mock = mock
                        .SetLegacyState(
                            agentAddress,
                            SerializeLegacyAgent(agentState))
                        .SetLegacyState(
                            avatarAddress,
                            MigrationAvatarState.LegacySerializeV1(avatarState));
                    break;
                case 2:
                    mock = mock
                        .SetLegacyState(
                            agentAddress,
                            SerializeLegacyAgent(agentState))
                        .SetLegacyState(
                            avatarAddress,
                            MigrationAvatarState.LegacySerializeV2(avatarState))
                        .SetLegacyState(
                            inventoryAddress,
                            avatarState.inventory.Serialize())
                        .SetLegacyState(
                            worldInformationAddress,
                            avatarState.questList.Serialize())
                        .SetLegacyState(
                            questListAddress,
                            avatarState.questList.Serialize());
                    break;
                default:
                    throw new ArgumentException($"Invalid legacy avatar version: {legacyAvatarVersion}");
            }

            if (alreadyMigrated)
            {
                mock = mock.SetAccount(
                    Addresses.Agent,
                    mock.GetAccount(Addresses.Agent)
                        .SetState(agentAddress, agentState.SerializeList()));
                mock = mock.SetAccount(
                    Addresses.Avatar,
                    mock.GetAccount(Addresses.Avatar)
                        .SetState(avatarAddress, avatarState.SerializeList()));
                mock = mock.SetAccount(
                    Addresses.Inventory,
                    mock.GetAccount(Addresses.Inventory)
                        .SetState(avatarAddress, avatarState.inventory.Serialize()));
                mock = mock.SetAccount(
                    Addresses.WorldInformation,
                    mock.GetAccount(Addresses.WorldInformation)
                        .SetState(avatarAddress, avatarState.worldInformation.Serialize()));
                mock = mock.SetAccount(
                    Addresses.QuestList,
                    mock.GetAccount(Addresses.QuestList)
                        .SetState(avatarAddress, avatarState.questList.Serialize()));
            }

            IAction action = new MigrateAgentAvatar
            {
                AgentAddresses = new List<Address> { agentAddress },
            };

            var plainValue = action.PlainValue;
            var actionLoader = new NCActionLoader();
            action = actionLoader.LoadAction(123, plainValue);

            IWorld nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = mock,
                    Miner = default,
                    Signer = new Address("e2D18a50472e93d3165c478DefA69fa149214E72"),
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

        private static IValue SerializeLegacyAgent(AgentState agentState)
        {
            var innerDict = new Dictionary<IKey, IValue>
            {
                [(Text)"avatarAddresses"] = new Dictionary(
                    agentState.avatarAddresses.Select(kv =>
                        new KeyValuePair<IKey, IValue>(
                            new Binary(BitConverter.GetBytes(kv.Key)),
                            kv.Value.Serialize()
                        )
                    )
                ),
                [(Text)"unlockedOptions"] = new List(),
                [(Text)LegacyAddressKey] = agentState.address.Serialize(),
            };
            if (agentState.MonsterCollectionRound > 0)
            {
                innerDict.Add((Text)MonsterCollectionRoundKey, agentState.MonsterCollectionRound.Serialize());
            }

            return new Dictionary(innerDict);
        }
    }
}
