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
        public List<(int id, int count)> Items;

        public IssueToken()
        {
        }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add(
                "values", Dictionary.Empty
                    .Add("a", AvatarAddress.Serialize())
                    .Add("f", new List(FungibleAssetValues.Select(f => f.Serialize())))
                    .Add("i", new List(Items.Select(i => List.Empty.Add(i.id).Add(i.count))))
            );

        public override void LoadPlainValue(IValue plainValue)
        {
            var dict = (Dictionary)((Dictionary)plainValue)["values"];
            AvatarAddress = dict["a"].ToAddress();
            FungibleAssetValues = ((List)dict["f"]).ToList(f => f.ToFungibleAssetValue());
            Items = new List<(int id, int count)>();
            var list = (List)dict["i"];
            foreach (var value in list)
            {
                var innerList = (List)value;
                Items.Add(((Integer)innerList[0], (Integer)innerList[1]));
            }
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            Addresses.CheckAvatarAddrIsContainedInAgent(context.Signer, AvatarAddress);
            var state = context.PreviousState;
            var sheet = state.GetSheet<LoadIntoMyGaragesCostSheet>();
            var garageCost = sheet.GetGarageCost(FungibleAssetValues, Items);
            state = state.TransferAsset(
                context,
                context.Signer,
                Addresses.GarageWallet,
                garageCost
            );
            foreach (var fungibleAssetValue in FungibleAssetValues)
            {
                var wrappedCurrency = Currencies.GetWrappedCurrency(fungibleAssetValue.Currency);
                state = state
                    .BurnAsset(context, context.Signer, fungibleAssetValue)
                    .MintAsset(
                        context,
                        context.Signer,
                        FungibleAssetValue.FromRawValue(wrappedCurrency, fungibleAssetValue.RawValue)
                    );
            }

            if (Items.Any())
            {
                var inventory = state.GetInventoryV2(AvatarAddress);
                foreach (var (id, count) in Items)
                {
                    if (!inventory.RemoveTradableMaterial(id, context.BlockIndex, count))
                    {
                        throw new NotEnoughItemException($"not enough tradable material({id})");
                    }

                    var wrappedCurrency = Currencies.GetItemCurrency(id, true);
                    state = state.MintAsset(context, context.Signer, count * wrappedCurrency);
                }
                state = state.SetInventory(AvatarAddress, inventory);
            }

            return state;
        }
    }
}
