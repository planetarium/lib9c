#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Exceptions;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [ActionType(TypeIdentifier)]
    public class MintAssets : ActionBase
    {
        public const string TypeIdentifier = "mint_assets";

        public MintAssets()
        {
        }

        public MintAssets(IEnumerable<MintSpec> specs, string? memo)
        {
            MintSpecs = specs.ToList();
            Memo = memo;
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            if (MintSpecs is null)
            {
                throw new InvalidOperationException();
            }

            IWorld state = context.PreviousState;
            HashSet<Address> allowed = new();

            if (state.TryGetLegacyState(Addresses.Admin, out Dictionary rawDict))
            {
                allowed.Add(new AdminState(rawDict).AdminAddress);
            }

            if (state.TryGetLegacyState(Addresses.AssetMinters, out List minters))
            {
                allowed.UnionWith(minters.Select(m => m.ToAddress()));
            }

            if (!allowed.Contains(context.Signer))
            {
                throw new InvalidMinterException(context.Signer);
            }

            Dictionary<Address, (List<FungibleAssetValue>, List<FungibleItemValue>)> mailRecords = new();

            foreach (var (recipient, assets, items) in MintSpecs)
            {
                if (!mailRecords.TryGetValue(recipient, out var records))
                {
                    mailRecords[recipient] = records = new(
                        new List<FungibleAssetValue>(),
                        new List<FungibleItemValue>()
                    );
                }

                (List<FungibleAssetValue> favs, List<FungibleItemValue> fivs) = records;

                if (assets is { } assetsNotNull)
                {
                    state = state.MintAsset(context, recipient, assetsNotNull);
                    favs.Add(assetsNotNull);
                }

                if (items is { } itemsNotNull)
                {
                    if (state.GetAvatarState(recipient) is AvatarState recipientAvatarState)
                    {
                        MaterialItemSheet itemSheet = state.GetSheet<MaterialItemSheet>();
                        if (itemSheet is null || itemSheet.OrderedList is null)
                        {
                            throw new InvalidOperationException();
                        }

                        foreach (MaterialItemSheet.Row row in itemSheet.OrderedList)
                        {
                            if (row.ItemId.Equals(itemsNotNull.Id))
                            {
                                Material item = ItemFactory.CreateMaterial(row);
                                recipientAvatarState.inventory.AddFungibleItem(item, itemsNotNull.Count);
                            }
                        }

                        state = state.SetAvatarState(recipient, recipientAvatarState);
                        fivs.Add(itemsNotNull);
                    }
                    else
                    {
                        throw new StateNullException(Addresses.Avatar, recipient);
                    }
                }
            }

            IRandom rng = context.GetRandom();
            foreach (var recipient in mailRecords.Keys)
            {
                if (state.GetAvatarState(recipient) is AvatarState recipientAvatarState)
                {
                    (List<FungibleAssetValue> favs, List<FungibleItemValue> fivs) = mailRecords[recipient];
                    List<(Address recipient, FungibleAssetValue v)> mailFavs =
                        favs.Select(v => (recipient, v))
                            .ToList();

                    if (mailRecords.TryGetValue(recipientAvatarState.agentAddress, out (List<FungibleAssetValue> agentFavs, List<FungibleItemValue> _) agentRecords))
                    {
                        mailFavs.AddRange(agentRecords.agentFavs.Select(v => (recipientAvatarState.agentAddress, v)));
                    }
                    recipientAvatarState.mailBox.Add(
                        new UnloadFromMyGaragesRecipientMail(
                            context.BlockIndex,
                            rng.GenerateRandomGuid(),
                            context.BlockIndex,
                            mailFavs,
                            fivs.Select(v => (v.Id, v.Count)),
                            Memo
                        )
                    );
                    recipientAvatarState.mailBox.CleanUp();
                    state = state.SetAvatarState(recipient, recipientAvatarState);
                }
            }

            return state;
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary)plainValue;
            var asList = (List)asDict["values"];

            if (asList[0] is Text memo)
            {
                Memo = memo;
            }
            else
            {
                Memo = null;
            }

            MintSpecs = asList.Skip(1).Select(v =>
            {
                return new MintSpec((List)v);
            }).ToList();
        }

        public override IValue PlainValue
        {
            get
            {
                var values = new List<IValue>
                {
                    Memo is { } memoNotNull ? (Text)memoNotNull : Null.Value
                };
                if (MintSpecs is { } mintSpecsNotNull)
                {
                    values.AddRange(mintSpecsNotNull.Select(s => s.Serialize()));
                }

                return new Dictionary(
                    new[]
                    {
                        new KeyValuePair<IKey, IValue>((Text)"type_id", (Text)TypeIdentifier),
                        new KeyValuePair<IKey, IValue>((Text)"values", new List(values))
                    }
                );
            }
        }

        public List<MintSpec>? MintSpecs
        {
            get;
            private set;
        }

        public string? Memo { get; private set; }

        public readonly struct MintSpec
        {
            public MintSpec(List bencoded)
                : this(
                    bencoded[0].ToAddress(),
                    bencoded[1] is List rawAssets ? rawAssets.ToFungibleAssetValue() : null,
                    bencoded[2] is List rawItems ? new FungibleItemValue(rawItems) : null
                )
            {
            }

            public MintSpec(
                Address recipient,
                FungibleAssetValue? assets,
                FungibleItemValue? items
            )
            {
                Recipient = recipient;
                Assets = assets;
                Items = items;
            }

            public IValue Serialize() => new List(
                Recipient.Serialize(),
                Assets?.Serialize() ?? Null.Value,
                Items?.Serialize() ?? Null.Value
            );

            internal void Deconstruct(
                out Address recipient,
                out FungibleAssetValue? assets,
                out FungibleItemValue? items
            )
            {
                recipient = Recipient;
                assets = Assets;
                items = Items;
            }

            public Address Recipient { get; }
            public FungibleAssetValue? Assets { get; }
            public FungibleItemValue? Items { get; }

        }
    }
}
