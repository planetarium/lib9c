using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Helper;
using Nekoyume.Model.Coupons;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action.Extensions
{
    public static class AccountExtensions
    {
        public static IAccount MarkBalanceChanged(
            this IAccount account,
            IActionContext context,
            Currency currency,
            params Address[] accounts
        )
        {
            if (accounts.Length == 1)
            {
                return account.MintAsset(context, accounts[0], currency * 1);
            }
            else if (accounts.Length < 1)
            {
                return account;
            }

            for (int i = 1; i < accounts.Length; i++)
            {
                account = account.TransferAsset(context, accounts[i - 1], accounts[i], currency * 1, true);
            }

            return account;
        }


        public static IAccount SetWorldBossKillReward(
            this IAccount account,
            IActionContext context,
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
                        account = account.MintAsset(context, agentAddress, reward);
                    }
                    else
                    {
                        account = account.MintAsset(context, avatarAddress, reward);
                    }
                }
            }

            return account.SetState(rewardInfoAddress, rewardRecord.Serialize());
        }

#nullable enable
        public static IAccount SetCouponWallet(
            this IAccount account,
            Address agentAddress,
            IImmutableDictionary<Guid, Coupon> couponWallet,
            bool rehearsal = false)
        {
            Address walletAddress = agentAddress.Derive(CouponWalletKey);
            if (rehearsal)
            {
                return account.SetState(walletAddress, ActionBase.MarkChanged);
            }

            IValue serializedWallet = new Bencodex.Types.List(
                couponWallet.Values.OrderBy(c => c.Id).Select(v => v.Serialize())
            );
            return account.SetState(walletAddress, serializedWallet);
        }
#nullable disable

        public static IAccount Mead(
            this IAccount account, IActionContext context, Address signer, BigInteger rawValue)
        {
            while (true)
            {
                var price = rawValue * Currencies.Mead;
                var balance = account.GetBalance(signer, Currencies.Mead);
                if (balance < price)
                {
                    var requiredMead = price - balance;
                    var contractAddress = signer.Derive(nameof(RequestPledge));
                    if (account.GetState(contractAddress) is List contract && contract[1].ToBoolean())
                    {
                        var patron = contract[0].ToAddress();
                        try
                        {
                            account = account.TransferAsset(context, patron, signer, requiredMead);
                        }
                        catch (InsufficientBalanceException)
                        {
                            account = account.Mead(context, patron, rawValue);
                            continue;
                        }
                    }
                    else
                    {
                        throw new InsufficientBalanceException("", signer, balance);
                    }
                }

                return account;
            }
        }
    }
}
