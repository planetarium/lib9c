using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [ActionType("activate_collection")]
    public class ActivateCollection: GameAction
    {
        public Address AvatarAddress;
        public int CollectionId;
        public List<Guid> ItemIdList = new();
        public override IAccount Execute(IActionContext context)
        {
            context.UseGas(1);
            var states = context.PreviousState;
            if (states.TryGetAvatarStateV2(context.Signer, AvatarAddress, out var avatarState,
                    out _))
            {
                var sheets = states.GetSheets(containItemSheet: true, sheetTypes: new[]
                {
                    typeof(CollectionSheet)
                });
                var collectionSheet = sheets.GetSheet<CollectionSheet>();
                var row = collectionSheet[CollectionId];
                var materialIds = row.Materials.Select(i => i.ItemId).ToList();
                foreach (var materialId in ItemIdList)
                {
                    avatarState.inventory.TryGetNonFungibleItem(materialId, out ItemUsable materialItem);
                    var itemId = materialItem.Id;
                    if (materialIds.Contains(itemId))
                    {
                        avatarState.inventory.RemoveNonFungibleItem(materialItem);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }

                var collectionAddress = CollectionState.Derive(AvatarAddress);
                var collectionState = states.TryGetState(collectionAddress, out List rawState)
                    ? new CollectionState(rawState)
                    : new CollectionState
                    {
                        Address = collectionAddress
                    };
                collectionState.Ids.Add(CollectionId);
                var inventoryAddress = AvatarAddress.Derive(LegacyInventoryKey);
                return states
                    .SetState(inventoryAddress, avatarState.inventory.Serialize())
                    .SetState(collectionAddress, collectionState.Serialize());
            }

            throw new FailedLoadStateException(AvatarAddress, typeof(Dictionary));
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["a"] = AvatarAddress.Serialize(),
                ["c"] = (Integer)CollectionId,
                ["i"] = new List(ItemIdList.Select(i => i.Serialize())),
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["a"].ToAddress();
            CollectionId = (Integer)plainValue["c"];
            var list = (List) plainValue["i"];
            ItemIdList = list.ToList(StateExtensions.ToGuid);
        }
    }
}
