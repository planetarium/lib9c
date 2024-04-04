namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using Xunit;

    public class RuneScenarioTest
    {
        [Fact]
        public void Craft_And_Unlock_And_Equip()
        {
            var agentAddress = new PrivateKey().Address;
            var agentState = new AgentState(agentAddress);
            var avatarAddress = new PrivateKey().Address;
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
            IWorld initialState = new World(MockUtil.MockModernWorldState)
                .SetAgentState(agentAddress, agentState)
                .SetAvatarState(avatarAddress, avatarState)
                .SetLegacyState(
                    Addresses.GoldCurrency,
                    new GoldCurrencyState(Currency.Legacy("NCG", 2, minters: null)).Serialize())
                .SetLegacyState(gameConfigState.address, gameConfigState.Serialize());
            foreach (var (key, value) in sheets)
            {
                initialState = initialState
                    .SetLegacyState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var runeId = 30001;
            var runeRow = tableSheets.RuneSheet[runeId];
            var rune = RuneHelper.ToCurrency(runeRow);
            initialState = initialState.MintAsset(context, avatarAddress, rune * 1);

            var runeAddress = RuneState.DeriveAddress(avatarAddress, runeId);
            Assert.Null(initialState.GetLegacyState(runeAddress));

            initialState = initialState.MintAsset(
                new ActionContext(),
                agentAddress,
                gameConfigState.RuneSkillSlotCrystalUnlockCost * Currencies.Crystal
            );

            var craftAction = new RuneEnhancement
            {
                AvatarAddress = avatarAddress,
                RuneId = runeId,
            };

            var prevState = craftAction.Execute(new ActionContext
            {
                BlockIndex = 1,
                PreviousState = initialState,
                RandomSeed = 0,
                Signer = agentAddress,
            });

            var runeState = Assert.IsType<RuneState>(prevState.GetRuneState(avatarAddress, runeId));

            Assert.Equal(1, runeState.Level);
            Assert.Equal(runeId, runeState.RuneId);

            var runeSlotStateAddress = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Adventure);
            Assert.Null(prevState.GetLegacyState(runeSlotStateAddress));

            var unlockAction = new UnlockRuneSlot
            {
                AvatarAddress = avatarAddress,
                SlotIndex = 6,
            };

            var state = unlockAction.Execute(new ActionContext
            {
                BlockIndex = 1,
                PreviousState = prevState,
                RandomSeed = 0,
                Signer = agentAddress,
            });

            var runeSlotState = new RuneSlotState((List)state.GetLegacyState(runeSlotStateAddress));
            Assert.Single(runeSlotState.GetRuneSlot().Where(r => r.RuneSlotType == RuneSlotType.Crystal && !r.IsLock));
            Assert.Single(runeSlotState.GetRuneSlot().Where(r => r.RuneSlotType == RuneSlotType.Crystal && r.IsLock));

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
                    new RuneSlotInfo(6, runeId),
                },
            };

            var nextState = has.Execute(new ActionContext
            {
                BlockIndex = 2,
                PreviousState = state,
                RandomSeed = 0,
                Signer = agentAddress,
            });

            var nextAvatarState = nextState.GetAvatarState(avatarAddress);
            Assert.True(nextAvatarState.worldInformation.IsStageCleared(1));
            var rawRuneSlot = Assert.IsType<List>(nextState.GetLegacyState(runeSlotStateAddress));
            var runeSlot = new RuneSlotState(rawRuneSlot);
            var runeSlotInfo = runeSlot.GetEquippedRuneSlotInfos().Single();

            Assert.Equal(runeId, runeSlotInfo.RuneId);
            Assert.Equal(6, runeSlotInfo.SlotIndex);
        }
    }
}
