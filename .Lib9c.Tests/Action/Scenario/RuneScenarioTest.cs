namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Extensions;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class RuneScenarioTest
    {
        [Fact]
        public void Craft_And_Equip()
        {
            var agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(agentAddress);
            var avatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = avatarAddress.Derive("ranking_map");
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            agentState.avatarAddresses.Add(0, avatarAddress);
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                gameConfigState,
                rankingMapAddress
            );

            var context = new ActionContext();
            IWorld initialState = new MockWorld();
            initialState = AgentModule.SetAgentState(initialState, agentAddress, agentState);
            initialState = AvatarModule.SetAvatarStateV2(initialState, avatarAddress, avatarState);
            initialState = LegacyModule.SetState(
                initialState,
                avatarAddress.Derive(LegacyInventoryKey),
                avatarState.inventory.Serialize());
            initialState = LegacyModule.SetState(
                initialState,
                avatarAddress.Derive(LegacyWorldInformationKey),
                avatarState.worldInformation.Serialize());
            initialState = LegacyModule.SetState(
                initialState,
                avatarAddress.Derive(LegacyQuestListKey),
                avatarState.questList.Serialize());
            initialState = LegacyModule.SetState(
                initialState,
                Addresses.GoldCurrency,
                new GoldCurrencyState(Currency.Legacy("NCG", 2, minters: null)).Serialize());
            initialState = LegacyModule.SetState(
                initialState,
                gameConfigState.address,
                gameConfigState.Serialize());
            foreach (var (key, value) in sheets)
            {
                initialState = LegacyModule.SetState(
                    initialState,
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            var runeId = 30001;
            var runeRow = tableSheets.RuneSheet[runeId];
            var rune = RuneHelper.ToCurrency(runeRow);
            initialState = LegacyModule.MintAsset(initialState, context, avatarAddress, rune * 1);

            var runeAddress = RuneState.DeriveAddress(avatarAddress, runeId);
            Assert.Null(LegacyModule.GetState(initialState, runeAddress));

            var craftAction = new RuneEnhancement
            {
                AvatarAddress = avatarAddress,
                RuneId = runeId,
            };

            var state = craftAction.Execute(
                new ActionContext
                {
                    BlockIndex = 1,
                    PreviousState = initialState,
                    Random = new TestRandom(),
                    Signer = agentAddress,
                });

            var rawRuneState = Assert.IsType<List>(LegacyModule.GetState(state, runeAddress));
            var runeState = new RuneState(rawRuneState);
            Assert.Equal(1, runeState.Level);
            Assert.Equal(runeId, runeState.RuneId);

            var runeSlotStateAddress = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Adventure);
            Assert.Null(LegacyModule.GetState(state, runeSlotStateAddress));

            var has = new HackAndSlash
            {
                StageId = 1,
                AvatarAddress = avatarAddress,
                Equipments = new List<Guid>(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
                WorldId = 1,
                RuneInfos = new List<RuneSlotInfo>
                {
                    new RuneSlotInfo(0, runeId),
                },
            };

            var nextState = has.Execute(
                new ActionContext
                {
                    BlockIndex = 2,
                    PreviousState = state,
                    Random = new TestRandom(),
                    Signer = agentAddress,
                });

            var nextAvatarState = AvatarModule.GetAvatarStateV2(nextState, avatarAddress);
            Assert.True(nextAvatarState.worldInformation.IsStageCleared(1));
            var rawRuneSlot = Assert.IsType<List>(LegacyModule.GetState(nextState, runeSlotStateAddress));
            var runeSlot = new RuneSlotState(rawRuneSlot);
            var runeSlotInfo = runeSlot.GetEquippedRuneSlotInfos().Single();

            Assert.Equal(runeId, runeSlotInfo.RuneId);
            Assert.Equal(0, runeSlotInfo.SlotIndex);
        }
    }
}
