using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("claim_raid_reward")]
    public class ClaimRaidReward: GameAction, IClaimRaidRewardV1
    {
        public Address AvatarAddress;

        Address IClaimRaidRewardV1.AvatarAddress => AvatarAddress;

        public ClaimRaidReward()
        {
        }

        public ClaimRaidReward(Address avatarAddress)
        {
            AvatarAddress = avatarAddress;
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            IWorld states = context.PreviousState;

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}ClaimRaidReward exec started", addressesHex);
            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(sheetTypes: new[] {
                typeof(RuneWeightSheet),
                typeof(WorldBossRankRewardSheet),
                typeof(WorldBossCharacterSheet),
                typeof(WorldBossListSheet),
                typeof(RuneSheet),
                typeof(MaterialItemSheet),
            });
            var worldBossListSheet = sheets.GetSheet<WorldBossListSheet>();
            int raidId;
            try
            {
                raidId = worldBossListSheet.FindRaidIdByBlockIndex(context.BlockIndex);
            }
            catch (InvalidOperationException)
            {
                // Find Latest raidId.
                raidId = worldBossListSheet.FindPreviousRaidIdByBlockIndex(context.BlockIndex);
            }
            var row = sheets.GetSheet<WorldBossListSheet>().Values.First(r => r.Id == raidId);
            var bossRow = sheets.GetSheet<WorldBossCharacterSheet>().Values.First(x => x.BossId == row.BossId);
            var raiderAddress = Addresses.GetRaiderAddress(AvatarAddress, raidId);
            RaiderState raiderState = states.GetRaiderState(raiderAddress);
            int rank = WorldBossHelper.CalculateRank(bossRow, raiderState.HighScore);
            var random = context.GetRandom();
            var inventory = states.GetInventoryV2(AvatarAddress);
            if (raiderState.LatestRewardRank < rank)
            {
                for (int i = raiderState.LatestRewardRank; i < rank; i++)
                {
                    var rewards = WorldBossHelper.CalculateReward(
                        i + 1,
                        row.BossId,
                        sheets.GetSheet<RuneWeightSheet>(),
                        sheets.GetSheet<WorldBossRankRewardSheet>(),
                        sheets.GetSheet<RuneSheet>(),
                        sheets.GetSheet<MaterialItemSheet>(),
                        random
                    );

                    foreach (var reward in rewards.assets)
                    {
                        if (reward.Currency.Equals(CrystalCalculator.CRYSTAL))
                        {
                            states = states.MintAsset(context, context.Signer, reward);
                        }
                        else
                        {
                            states = states.MintAsset(context, AvatarAddress, reward);
                        }
                    }

#pragma warning disable LAA1002
                    foreach (var reward in rewards.materials)
#pragma warning restore LAA1002
                    {
                        inventory.AddItem(reward.Key, reward.Value);
                    }
                }

                raiderState.LatestRewardRank = rank;
                raiderState.ClaimedBlockIndex = context.BlockIndex;
                states = states
                    .SetLegacyState(raiderAddress, raiderState.Serialize())
                    .SetInventory(AvatarAddress, inventory);
                var ended = DateTimeOffset.UtcNow;
                Log.Debug("{AddressesHex}ClaimRaidReward Total Executed Time: {Elapsed}", addressesHex, ended - started);
                return states;
            }

            throw new NotEnoughRankException();
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
                {
                    ["a"] = AvatarAddress.Serialize(),
                }
                .ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
        }
    }
}
