using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Model.Collection;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("activate_collection")]
    public class ActivateCollection: GameAction
    {
        public Address AvatarAddress;
        public int CollectionId;
        public List<ICollectionMaterial> Materials = new();
        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            if (states.TryGetAvatarState(context.Signer, AvatarAddress, out var avatarState))
            {
                var sheets = states.GetSheets(containItemSheet: true, sheetTypes: new[]
                {
                    typeof(CollectionSheet)
                });
                var collectionSheet = sheets.GetSheet<CollectionSheet>();
                var row = collectionSheet[CollectionId];
                var materials = Materials;
                foreach (var materialInfo in row.Materials)
                {
                    var material = materials.FirstOrDefault(m =>
                        m.ItemId == materialInfo.ItemId && m.ItemCount == materialInfo.Count);
                    if (material is null)
                    {
                        throw new Exception();
                    }
                    switch (material)
                    {
                        case FungibleCollectionMaterial fungibleCollectionMaterial:
                            if (!avatarState.inventory.RemoveMaterial(materialInfo.ItemId,
                                    materialInfo.Count))
                            {
                                throw new Exception();
                            }
                            break;
                        case NonFungibleCollectionMaterial nonFungibleCollectionMaterial:
                            var nonFungibleId = nonFungibleCollectionMaterial.NonFungibleId;
                            if (avatarState.inventory.TryGetNonFungibleItem(nonFungibleId,
                                    out ItemUsable materialItem) && materialInfo.Validate(materialItem))
                            {
                                avatarState.inventory.RemoveNonFungibleItem(materialItem);
                            }
                            else
                            {
                                throw new Exception();
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(material));
                    }

                    materials.Remove(material);
                }

                if (materials.Any())
                {
                    throw new Exception();
                }

                CollectionState collectionState;
                try
                {
                    collectionState = states.GetCollectionState(AvatarAddress);
                }
                catch (FailedLoadStateException)
                {
                    collectionState = new CollectionState();
                }
                catch (InvalidCastException)
                {
                    collectionState = new CollectionState();
                }
                collectionState.Ids.Add(CollectionId);
                return states
                    .SetAvatarState(AvatarAddress, avatarState, false, true, false, false)
                    .SetCollectionState(AvatarAddress, collectionState);
            }

            throw new FailedLoadStateException(AvatarAddress, typeof(Dictionary));
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["c"] = (Integer)CollectionId,
                ["m"] = new List(Materials.Select(i => i.Serialize())),
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            CollectionId = (Integer)plainValue["c"];
            var list = (List) plainValue["m"];
            foreach (var innerList in list)
            {
                Materials.Add(CollectionFactory.DeserializeMaterial((List)innerList));
            }
        }
    }
}
