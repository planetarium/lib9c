using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using LruCacheNet;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    public static class AccountStateDeltaExtensions
    {
        private const int SheetsCacheSize = 100;
        private static readonly LruCache<string, ISheet> SheetsCache = new LruCache<string, ISheet>(SheetsCacheSize);

        public static IAccountStateDelta MarkBalanceChanged(
            this IAccountStateDelta states,
            Currency currency,
            params Address[] accounts
        )
        {
            if (accounts.Length == 1)
            {
                return states.MintAsset(accounts[0], currency * 1);
            }
            else if (accounts.Length < 1)
            {
                return states;
            }

            for (int i = 1; i < accounts.Length; i++)
            {
                states = states.TransferAsset(accounts[i - 1], accounts[i], currency * 1, true);
            }

            return states;
        }


        public static IAccountStateDelta SetWorldBossKillReward(
            this IAccountStateDelta states,
            Address rewardInfoAddress,
            WorldBossKillRewardRecord rewardRecord,
            int rank,
            WorldBossState bossState,
            RuneWeightSheet runeWeightSheet,
            WorldBossKillRewardSheet worldBossKillRewardSheet,
            RuneSheet runeSheet,
            IRandom random,
            Address avatarAddress,
            Address agentAddress)
        {
            if (!rewardRecord.IsClaimable(bossState.Level))
            {
                throw new InvalidClaimException();
            }
#pragma warning disable LAA1002
            var filtered = rewardRecord
                .Where(kv => !kv.Value)
                .Select(kv => kv.Key)
                .ToList();
#pragma warning restore LAA1002
            foreach (var level in filtered)
            {
                List<FungibleAssetValue> rewards = RuneHelper.CalculateReward(
                    rank,
                    bossState.Id,
                    runeWeightSheet,
                    worldBossKillRewardSheet,
                    runeSheet,
                    random
                );
                rewardRecord[level] = true;
                foreach (var reward in rewards)
                {
                    if (reward.Currency.Equals(CrystalCalculator.CRYSTAL))
                    {
                        states = states.MintAsset(agentAddress, reward);
                    }
                    else
                    {
                        states = states.MintAsset(avatarAddress, reward);
                    }
                }
            }

            return states.SetState(rewardInfoAddress, rewardRecord.Serialize());
        }

        public static IAccountStateDelta SetState(
            this IAccountStateDelta states,
            Address address,
            Text moniker,
            Integer version,
            IValue data)
        {
            return states.SetState(
                address,
                VersionedStateFormatter.Serialize(
                    moniker,
                    version,
                    data));
        }

        public static IAccountStateDelta SetState(
            this IAccountStateDelta states,
            Address address,
            string moniker,
            uint version,
            IValue data)
        {
            return states.SetState(
                address,
                VersionedStateFormatter.Serialize(
                    moniker,
                    version,
                    data));
        }

        public static IAccountStateDelta SetState(
            this IAccountStateDelta states,
            Address address,
            string moniker,
            uint version,
            IState state)
        {
            return states.SetState(
                address,
                VersionedStateFormatter.Serialize(
                    moniker,
                    version,
                    state));
        }

        public static IAccountStateDelta SetState<T>(
            this IAccountStateDelta states,
            Address address,
            T state)
            where T : IState
        {
            if (!VersionedStateFormatter.TryDeconstruct(
                    state,
                    out var result))
            {
                return states.SetState(address, state.Serialize());
            }

            return states.SetState(
                address,
                result.moniker,
                result.version,
                result.data);
        }
    }
}
