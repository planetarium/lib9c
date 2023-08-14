using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Action.Extensions;
using Nekoyume.Model.State;

namespace Lib9c.DevExtensions.Action
{
    [Serializable]
    [ActionType("manipulate_state")]
    public class ManipulateState : GameAction
    {
        public List<(Address addr, IValue value)> StateList { get; set; }
        public List<(Address addr, FungibleAssetValue fav)> BalanceList { get; set; }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["sl"] = StateList.Serialize(),
                ["bl"] = new List(BalanceList
                    .OrderBy(tuple => tuple.addr)
                    .ThenBy(tuple => tuple.fav.Currency.Ticker)
                    .ThenBy(tuple => tuple.fav.Currency.DecimalPlaces)
                    .ThenBy(tuple => tuple.fav.Currency.Minters)
                    .ThenBy(tuple => tuple.fav.RawValue)
                    .Select(tuple => new List(
                        tuple.addr.Serialize(),
                        tuple.fav.Serialize()))),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            StateList = plainValue["sl"].ToStateList();
            BalanceList = ((List)plainValue["bl"])
                .OfType<List>()
                .Select(list => (list[0].ToAddress(), list[1].ToFungibleAssetValue()))
                .ToList();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            if (context.Rehearsal)
            {
                return context.PreviousState;
            }

            return Execute(context, context.PreviousState, StateList, BalanceList);
        }

        public static IWorld Execute(
            IActionContext context,
            IWorld world,
            List<(Address addr, IValue value)> stateList,
            List<(Address addr, FungibleAssetValue fav)> balanceList)
        {
            var account = world.GetAccount(ReservedAddresses.LegacyAccount);
            foreach (var (addr, value) in stateList)
            {
                account = account.SetState(addr, value);
            }

            var ncg = account.GetGoldCurrency();
            foreach (var (addr, fav) in balanceList)
            {
                var currentFav = account.GetBalance(addr, fav.Currency);
                if (currentFav == fav)
                {
                    continue;
                }

                if (fav.Currency.Minters?.Any() ?? false)
                {
                    if (fav.Currency.Equals(ncg))
                    {
                        if (currentFav > fav)
                        {
                            account = account.TransferAsset(
                                context,
                                addr,
                                GoldCurrencyState.Address,
                                currentFav - fav);
                        }
                        else
                        {
                            account = account.TransferAsset(
                                context,
                                GoldCurrencyState.Address,
                                addr,
                                fav - currentFav);
                        }

                        continue;
                    }

                    throw new NotSupportedException($"{fav.Currency} is not supported.");
                }

                account = currentFav > fav
                    ? account.BurnAsset(context, addr, currentFav - fav)
                    : account.MintAsset(context, addr, fav - currentFav);
            }

            return world.SetAccount(account);
        }
    }
}
