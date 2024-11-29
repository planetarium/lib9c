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
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [ActionType(ActionTypeText)]
    public class ActivateCollection : GameAction
    {
        private const string ActionTypeText = "activate_collection";
        private const int MaxCollectionDataCount = 10;
        public Address AvatarAddress;

        public List<(int collectionId, List<ICollectionMaterial> materials)> CollectionData = new();

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            if (CollectionData.Count > MaxCollectionDataCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(CollectionData),
                    CollectionData.Count,
                    $"CollectionData count exceeds the {MaxCollectionDataCount}");
            }

            var states = context.PreviousState;
            if (states.TryGetAvatarState(context.Signer, AvatarAddress, out var avatarState))
            {
                var sheets = states.GetSheets(containItemSheet: true,
                    sheetTypes: new[]
                    {
                        typeof(CollectionSheet),
                    });
                var collectionSheet = sheets.GetSheet<CollectionSheet>();
                var collectionState = states.TryGetCollectionState(AvatarAddress, out var state)
                    ? state
                    : new CollectionState();
                var itemSheet = sheets.GetItemSheet();
                foreach (var (collectionId, collectionMaterials) in CollectionData)
                {
                    if (collectionState.Ids.Contains(collectionId))
                    {
                        throw new AlreadyActivatedException($"{collectionId} already activated.");
                    }
                    var row = collectionSheet[collectionId];
                    foreach (var requiredMaterial in row.Materials)
                    {
                        ICollectionMaterial registeredMaterial = requiredMaterial.GetMaterial(collectionMaterials);
                        ItemSheet.Row itemRow = itemSheet[registeredMaterial.ItemId];
                        switch (registeredMaterial)
                        {
                            case FungibleCollectionMaterial fungibleCollectionMaterial:
                                fungibleCollectionMaterial.BurnMaterial(itemRow, avatarState.inventory, context.BlockIndex);
                                break;
                            case NonFungibleCollectionMaterial nonFungibleCollectionMaterial:
                                nonFungibleCollectionMaterial.BurnMaterial(itemRow, avatarState.inventory, requiredMaterial, context.BlockIndex);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(registeredMaterial));
                        }

                        collectionMaterials.Remove(registeredMaterial);
                    }

                    if (collectionMaterials.Any())
                    {
                        throw new ArgumentOutOfRangeException(
                            $"material does not match collection {row.Id}");
                    }

                    collectionState.Ids.Add(collectionId);
                }
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
                ["c"] = new List(
                    CollectionData
                        .Select(c => List.Empty
                            .Add((Integer) c.collectionId)
                            .Add(new List(c.materials.Select(i => i.Bencoded))))
                ),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            var list = (List) plainValue["c"];
            foreach (var value in list)
            {
                var innerList = (List) value;
                CollectionData.Add(((Integer) innerList[0],
                    ((List) innerList[1]).Select(i => CollectionFactory.DeserializeMaterial((List) i))
                    .ToList()));
            }
        }
    }
}
