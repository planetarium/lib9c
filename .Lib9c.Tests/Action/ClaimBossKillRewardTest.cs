namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class ClaimBossKillRewardTest
    {
        [Theory]
        [InlineData(200L, typeof(InvalidClaimException))]
        [InlineData(200L, null)]
        [InlineData(1L, null)]
        [InlineData(1L, typeof(InvalidClaimException))]
        public void Execute(long blockIndex, Type exc)
        {
            Address avatarAddress = new PrivateKey().ToAddress();
            IAccountStateDelta state = new State();

            var runeWeightSheet = new RuneWeightSheet();
            runeWeightSheet.Set(@"id,boss_id,rank,rune_id,weight
1,205007,0,1,100
");
            var killRewardSheet = new WorldBossKillRewardSheet();
            killRewardSheet.Set(@"id,boss_id,rank,rune_min,rune_max,crystal
1,205007,0,1,1,100
");
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var worldBossListSheet = new WorldBossListSheet();
            worldBossListSheet.Set(@"id,boss_id,started_block_index,ended_block_index,fee,ticket_price,additional_ticket_price,max_purchase_count
1,205007,0,100,300,200,100,10
");
            var worldBossKillRewardRecordAddress = Addresses.GetWorldBossKillRewardRecordAddress(avatarAddress, 1);
            var worldBossKillRewardRecord = new WorldBossKillRewardRecord();
            if (exc is null)
            {
                worldBossKillRewardRecord[0] = new List<FungibleAssetValue>();
            }

            var raiderStateAddress = Addresses.GetRaiderAddress(avatarAddress, 1);
            var raiderState = new RaiderState();

            var worldBossAddress = Addresses.GetWorldBossAddress(1);
            var worldBossState = new WorldBossState(worldBossListSheet[1], tableSheets.WorldBossGlobalHpSheet[1]);

            state = state
                .SetState(Addresses.GetSheetAddress<RuneWeightSheet>(), runeWeightSheet.Serialize())
                .SetState(Addresses.GetSheetAddress<WorldBossListSheet>(), worldBossListSheet.Serialize())
                .SetState(Addresses.GetSheetAddress<WorldBossKillRewardSheet>(), killRewardSheet.Serialize())
                .SetState(Addresses.GetSheetAddress<RuneSheet>(), tableSheets.RuneSheet.Serialize())
                .SetState(worldBossKillRewardRecordAddress, worldBossKillRewardRecord.Serialize())
                .SetState(worldBossAddress, worldBossState.Serialize())
                .SetState(raiderStateAddress, raiderState.Serialize());

            var action = new ClaimWordBossKillReward
            {
                AvatarAddress = avatarAddress,
            };

            if (exc is null)
            {
                var nextState = action.Execute(new ActionContext
                {
                    BlockIndex = blockIndex,
                    Signer = default,
                    PreviousStates = state,
                    Random = new TestRandom(),
                });

                var runeCurrency = RuneHelper.ToCurrency(tableSheets.RuneSheet[1], 0, null);
                Assert.Equal(1 * runeCurrency, nextState.GetBalance(avatarAddress, runeCurrency));
                Assert.Equal(100 * CrystalCalculator.CRYSTAL, nextState.GetBalance(avatarAddress, CrystalCalculator.CRYSTAL));
                var nextRewardInfo = new WorldBossKillRewardRecord((List)nextState.GetState(worldBossKillRewardRecordAddress));
                Assert.All(nextRewardInfo, kv => Assert.NotEmpty(kv.Value));
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext
                {
                    BlockIndex = blockIndex,
                    Signer = default,
                    PreviousStates = state,
                    Random = new TestRandom(),
                }));
            }
        }
    }
}
