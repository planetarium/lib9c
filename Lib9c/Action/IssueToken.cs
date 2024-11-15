using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Exceptions;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData.Garages;

namespace Nekoyume.Action
{
    [ActionType(TypeIdentifier)]
    public class IssueToken : ActionBase
    {
        public const string TypeIdentifier = "issue_token";
        public Address AvatarAddress;
        public List<FungibleAssetValue> FungibleAssetValues;
        public List<(int id, int count, bool tradable)> Items;

        public IssueToken()
        {
        }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add(
                "values", Dictionary.Empty
                    .Add("a", AvatarAddress.Serialize())
                    .Add("f", new List(FungibleAssetValues.Select(f => f.Serialize())))
                    .Add("i", new List(Items.Select(i => List.Empty.Add(i.id).Add(i.count).Add(i.tradable.Serialize()))))
            );

        public override void LoadPlainValue(IValue plainValue)
        {
            var dict = (Dictionary)((Dictionary)plainValue)["values"];
            AvatarAddress = dict["a"].ToAddress();
            FungibleAssetValues = ((List)dict["f"]).ToList(f => f.ToFungibleAssetValue());
            Items = new List<(int id, int count, bool tradable)>();
            var list = (List)dict["i"];
            foreach (var value in list)
            {
                var innerList = (List)value;
                Items.Add(((Integer)innerList[0], (Integer)innerList[1], innerList[2].ToBoolean()));
            }
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            Addresses.CheckAvatarAddrIsContainedInAgent(context.Signer, AvatarAddress);
            if (!FungibleAssetValues.Any() && !Items.Any())
            {
                throw new InvalidActionFieldException("either FungibleAssetValues or Items must be set.");
            }

            var state = context.PreviousState;
            foreach (var fungibleAssetValue in FungibleAssetValues)
            {
                if (fungibleAssetValue.Sign < 0)
                {
                    throw new InvalidActionFieldException(
                        "FungibleAssetValue.Sign must be greater than 0.");
                }

                var currency = fungibleAssetValue.Currency;
                if (currency.Minters is not null)
                {
                    throw new InvalidCurrencyException("only minterless currency is allowed.");
                }
                var wrappedCurrency = Currencies.GetWrappedCurrency(currency);
                var address = Currencies.PickAddress(currency, context.Signer, AvatarAddress);
                state = state
                    .BurnAsset(context, address, fungibleAssetValue)
                    .MintAsset(
                        context,
                        context.Signer,
                        FungibleAssetValue.FromRawValue(wrappedCurrency, fungibleAssetValue.RawValue)
                    );
            }

            if (Items.Any())
            {
                var inventory = state.GetInventoryV2(AvatarAddress);
                foreach (var (id, count, tradable) in Items)
                {
                    if (count < 0)
                    {
                        throw new InvalidActionFieldException("item count must be greater than 0.");
                    }

                    if (tradable)
                    {
                        if (!inventory.RemoveTradableMaterial(id, context.BlockIndex, count))
                        {
                            throw new NotEnoughItemException($"not enough tradable material({id})");
                        }
                    }
                    else
                    {
                        if (!inventory.RemoveNonTradableMaterial(id, count))
                        {
                            throw new NotEnoughItemException($"not enough non-tradable material({id})");
                        }
                    }

                    var wrappedCurrency = Currencies.GetItemCurrency(id, tradable);
                    state = state.MintAsset(context, context.Signer, count * wrappedCurrency);
                }
                state = state.SetInventory(AvatarAddress, inventory);
            }

            var sheet = state.GetSheet<LoadIntoMyGaragesCostSheet>();
            var garageCost = sheet.GetGarageCost(FungibleAssetValues, Items);
            return state.TransferAsset(
                context,
                context.Signer,
                Addresses.GarageWallet,
                garageCost
            );
        }
    }
}
