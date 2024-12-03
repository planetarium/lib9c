using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.DevExtensions.Model;
using Lib9c.Model.Order;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace Lib9c.DevExtensions.Action
{
    [Serializable]
    [ActionType("create_testbed")]
    public class CreateTestbed : GameAction
    {
        private int _slotIndex = 0;
        private PrivateKey _privateKey = new PrivateKey();
        public Result result = new Result();
        public List<Order> Orders = new List<Order>();
        public Address weeklyArenaAddress;

        [Serializable]
        public class Result
        {
            public Address SellerAgentAddress;
            public Address SellerAvatarAddress;
            public List<ItemInfos> ItemInfos;
        }

        public class ItemInfos
        {
            public Guid OrderId;
            public Guid TradableId;
            public ItemSubType ItemSubType;
            public BigInteger Price;
            public int Count;

            public ItemInfos(Guid orderId, Guid tradableId, ItemSubType itemSubType, BigInteger price, int count)
            {
                OrderId = orderId;
                TradableId = tradableId;
                ItemSubType = itemSubType;
                Price = price;
                Count = count;
            }
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>()
            {
                { "w", weeklyArenaAddress.Serialize() },
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            weeklyArenaAddress = plainValue["w"].ToAddress();
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var sellData = TestbedHelper.LoadData<TestbedSell>("TestbedSell");
            var random = context.GetRandom();
            var addedItemInfos = sellData.Items
                .Select(item => new TestbedHelper.AddedItemInfo(
                    random.GenerateRandomGuid(),
                    random.GenerateRandomGuid()))
                .ToList();

            var agentAddress = _privateKey.PublicKey.Address;
            var states = context.PreviousState;

            var avatarAddress = agentAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CreateAvatar.DeriveFormat,
                    _slotIndex
                )
            );
            var orderReceiptAddress = OrderDigestListState.DeriveAddress(avatarAddress);

            // Create Agent and avatar
            var existingAgentState = states.GetAgentState(agentAddress);
            var agentState = existingAgentState ?? new AgentState(agentAddress);
            var avatarState = states.GetAvatarState(avatarAddress);
            if (!(avatarState is null))
            {
                throw new InvalidAddressException(
                    $"Aborted as there is already an avatar at {avatarAddress}.");
            }

            if (agentState.avatarAddresses.ContainsKey(_slotIndex))
            {
                throw new AvatarIndexAlreadyUsedException(
                    $"Aborted as the signer already has an avatar at index #{_slotIndex}.");
            }

            agentState.avatarAddresses.Add(_slotIndex, avatarAddress);

            var rankingState = context.PreviousState.GetRankingState();
            var rankingMapAddress = rankingState.UpdateRankingMap(avatarAddress);
            avatarState = TestbedHelper.CreateAvatarState(sellData.Avatar.Name,
                agentAddress,
                avatarAddress,
                context.BlockIndex,
                context.PreviousState.GetAvatarSheets(),
                context.PreviousState.GetSheet<WorldSheet>(),
                context.PreviousState.GetGameConfigState(),
                rankingMapAddress);

            // Add item
            var costumeItemSheet = context.PreviousState.GetSheet<CostumeItemSheet>();
            var equipmentItemSheet = context.PreviousState.GetSheet<EquipmentItemSheet>();
            var optionSheet = context.PreviousState.GetSheet<EquipmentItemOptionSheet>();
            var skillSheet = context.PreviousState.GetSheet<SkillSheet>();
            var materialItemSheet = context.PreviousState.GetSheet<MaterialItemSheet>();
            var consumableItemSheet = context.PreviousState.GetSheet<ConsumableItemSheet>();
            for (var i = 0; i < sellData.Items.Length; i++)
            {
                TestbedHelper.AddItem(costumeItemSheet,
                    equipmentItemSheet,
                    optionSheet,
                    skillSheet,
                    materialItemSheet,
                    consumableItemSheet,
                    random,
                    sellData.Items[i], addedItemInfos[i], avatarState);
            }

            avatarState.Customize(0, 0, 0, 0);

            var allCombinationSlotState = new AllCombinationSlotState();
            for (var i = 0; i < AvatarState.DefaultCombinationSlotCount; i++)
            {
                var slotAddr = Addresses.GetCombinationSlotAddress(avatarAddress, i);
                var slot = new CombinationSlotState(slotAddr, i);
                allCombinationSlotState.AddSlot(slot);
            }

            states = states.SetCombinationSlotState(avatarAddress, allCombinationSlotState);

            avatarState.UpdateQuestRewards(materialItemSheet);
            states = states
                .SetAgentState(agentAddress, agentState)
                .SetLegacyState(Addresses.Ranking, rankingState.Serialize())
                .SetAvatarState(avatarAddress, avatarState);
            // ~Create Agent and avatar && ~Add item

            // for sell
            var costumeStatSheet = states.GetSheet<CostumeStatSheet>();
            for (var i = 0; i < sellData.Items.Length; i++)
            {
                var itemAddress = Addresses.GetItemAddress(addedItemInfos[i].TradableId);
                var orderAddress = Order.DeriveAddress(addedItemInfos[i].OrderId);
                var shopAddress = ShardedShopStateV2.DeriveAddress(
                    sellData.Items[i].ItemSubType,
                    addedItemInfos[i].OrderId);

                var balance =
                    context.PreviousState.GetBalance(agentAddress, states.GetGoldCurrency());
                var price = new FungibleAssetValue(balance.Currency, sellData.Items[i].Price, 0);
                var order = OrderFactory.Create(agentAddress, avatarAddress,
                    addedItemInfos[i].OrderId,
                    price,
                    addedItemInfos[i].TradableId,
                    context.BlockIndex,
                    sellData.Items[i].ItemSubType,
                    sellData.Items[i].Count);

                Orders.Add(order);
                order.Validate(avatarState, sellData.Items[i].Count);
                var tradableItem = order.Sell(avatarState);

                var shardedShopState =
                    states.TryGetLegacyState(shopAddress, out Dictionary serializedState)
                        ? new ShardedShopStateV2(serializedState)
                        : new ShardedShopStateV2(shopAddress);
                var orderDigest = order.Digest(avatarState, costumeStatSheet);
                shardedShopState.Add(orderDigest, context.BlockIndex);
                var orderReceiptList =
                    states.TryGetLegacyState(orderReceiptAddress, out Dictionary receiptDict)
                        ? new OrderDigestListState(receiptDict)
                        : new OrderDigestListState(orderReceiptAddress);
                orderReceiptList.Add(orderDigest);

                states = states.SetLegacyState(orderReceiptAddress, orderReceiptList.Serialize())
                    .SetAvatarState(avatarAddress, avatarState)
                    .SetLegacyState(itemAddress, tradableItem.Serialize())
                    .SetLegacyState(orderAddress, order.Serialize())
                    .SetLegacyState(shopAddress, shardedShopState.Serialize());
            }

            result.SellerAgentAddress = agentAddress;
            result.SellerAvatarAddress = avatarAddress;
            result.ItemInfos = new List<ItemInfos>();
            for (var i = 0; i < sellData.Items.Length; i++)
            {
                result.ItemInfos.Add(new ItemInfos(
                    addedItemInfos[i].OrderId,
                    addedItemInfos[i].TradableId,
                    sellData.Items[i].ItemSubType,
                    sellData.Items[i].Price,
                    sellData.Items[i].Count));
            }

            return states;
        }
    }
}
