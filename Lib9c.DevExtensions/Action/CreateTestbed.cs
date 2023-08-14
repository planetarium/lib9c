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
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Action.Extensions;
using Nekoyume.Model.Exceptions;
using Nekoyume.Model.Item;
using Nekoyume.Module;

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
                {"w", weeklyArenaAddress.Serialize()},
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            weeklyArenaAddress = plainValue["w"].ToAddress();
        }

        public override IWorld Execute(IActionContext context)
        {
            context.UseGas(1);
            var sellData = TestbedHelper.LoadData<TestbedSell>("TestbedSell");
            var addedItemInfos = sellData.Items
                .Select(item => new TestbedHelper.AddedItemInfo(
                    context.Random.GenerateRandomGuid(),
                    context.Random.GenerateRandomGuid()))
                .ToList();

            var agentAddress = _privateKey.PublicKey.ToAddress();
            var world = context.PreviousState;

            var avatarAddress = agentAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CreateAvatar.DeriveFormat,
                    _slotIndex
                )
            );
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            var orderReceiptAddress = OrderDigestListState.DeriveAddress(avatarAddress);

            if (context.Rehearsal)
            {
                world = LegacyModule.SetState(world, agentAddress, MarkChanged);
                for (var i = 0; i < AvatarState.CombinationSlotCapacity; i++)
                {
                    var slotAddress = avatarAddress.Derive(
                        string.Format(CultureInfo.InvariantCulture,
                            CombinationSlotState.DeriveFormat, i));
                    world = LegacyModule.SetState(world, slotAddress, MarkChanged);
                }

                world = LegacyModule.SetState(world, avatarAddress, MarkChanged);
                world = LegacyModule.SetState(world, Addresses.Ranking, MarkChanged);
                world = LegacyModule.SetState(world, worldInformationAddress, MarkChanged);
                world = LegacyModule.SetState(world, questListAddress, MarkChanged);
                world = LegacyModule.SetState(world, inventoryAddress, MarkChanged);

                for (var i = 0; i < sellData.Items.Length; i++)
                {
                    var itemAddress = Addresses.GetItemAddress(addedItemInfos[i].TradableId);
                    var orderAddress = Order.DeriveAddress(addedItemInfos[i].OrderId);
                    var shopAddress = ShardedShopStateV2.DeriveAddress(
                        sellData.Items[i].ItemSubType,
                        addedItemInfos[i].OrderId);

                    world = LegacyModule.SetState(world, avatarAddress, MarkChanged);
                    world = LegacyModule.SetState(world, inventoryAddress, MarkChanged);
                    world = LegacyModule.MarkBalanceChanged(
                        world,
                        context,
                        GoldCurrencyMock,
                        agentAddress,
                        GoldCurrencyState.Address);
                    world = LegacyModule.SetState(world, orderReceiptAddress, MarkChanged);
                    world = LegacyModule.SetState(world, itemAddress, MarkChanged);
                    world = LegacyModule.SetState(world, orderAddress, MarkChanged);
                    world = LegacyModule.SetState(world, shopAddress, MarkChanged);
                }

                return world;
            }

            // Create Agent and avatar
            var existingAgentState = AgentModule.GetAgentState(world, agentAddress);
            var agentState = existingAgentState ?? new AgentState(agentAddress);
            var avatarState = AvatarModule.GetAvatarState(world, avatarAddress);
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

            var rankingState = LegacyModule.GetRankingState(context.PreviousState);
            var rankingMapAddress = rankingState.UpdateRankingMap(avatarAddress);
            avatarState = TestbedHelper.CreateAvatarState(sellData.Avatar.Name,
                agentAddress,
                avatarAddress,
                context.BlockIndex,
                LegacyModule.GetAvatarSheets(context.PreviousState),
                LegacyModule.GetSheet<WorldSheet>(context.PreviousState),
                LegacyModule.GetGameConfigState(context.PreviousState),
                rankingMapAddress);

            // Add item
            var costumeItemSheet =  LegacyModule.GetSheet<CostumeItemSheet>(context.PreviousState);
            var equipmentItemSheet = LegacyModule.GetSheet<EquipmentItemSheet>(context.PreviousState);
            var optionSheet = LegacyModule.GetSheet<EquipmentItemOptionSheet>(context.PreviousState);
            var skillSheet = LegacyModule.GetSheet<SkillSheet>(context.PreviousState);
            var materialItemSheet = LegacyModule.GetSheet<MaterialItemSheet>(context.PreviousState);
            var consumableItemSheet = LegacyModule.GetSheet<ConsumableItemSheet>(context.PreviousState);
            for (var i = 0; i < sellData.Items.Length; i++)
            {
                TestbedHelper.AddItem(costumeItemSheet,
                    equipmentItemSheet,
                    optionSheet,
                    skillSheet,
                    materialItemSheet,
                    consumableItemSheet,
                    context.Random,
                    sellData.Items[i], addedItemInfos[i], avatarState);
            }

            avatarState.Customize(0, 0, 0, 0);

            foreach (var address in avatarState.combinationSlotAddresses)
            {
                var slotState =
                    new CombinationSlotState(address,
                        GameConfig.RequireClearedStageLevel.CombinationEquipmentAction);
                world = LegacyModule.SetState(world, address, slotState.Serialize());
            }

            avatarState.UpdateQuestRewards(materialItemSheet);
            world = AgentModule.SetAgentState(world, agentAddress, agentState);
            world = LegacyModule.SetState(world, Addresses.Ranking, rankingState.Serialize());
            world = LegacyModule.SetState(world, inventoryAddress, avatarState.inventory.Serialize());
            world = LegacyModule.SetState(world, worldInformationAddress, avatarState.worldInformation.Serialize());
            world = LegacyModule.SetState(world, questListAddress, avatarState.questList.Serialize());
            world = AvatarModule.SetAvatarStateV2(world, avatarAddress, avatarState);
            // ~Create Agent and avatar && ~Add item

            // for sell
            var costumeStatSheet = LegacyModule.GetSheet<CostumeStatSheet>(world);
            for (var i = 0; i < sellData.Items.Length; i++)
            {
                var itemAddress = Addresses.GetItemAddress(addedItemInfos[i].TradableId);
                var orderAddress = Order.DeriveAddress(addedItemInfos[i].OrderId);
                var shopAddress = ShardedShopStateV2.DeriveAddress(
                    sellData.Items[i].ItemSubType,
                    addedItemInfos[i].OrderId);

                var balance = LegacyModule.GetBalance(
                    context.PreviousState,
                    agentAddress,
                    LegacyModule.GetGoldCurrency(world));
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
                    LegacyModule.TryGetState(world, shopAddress, out Dictionary serializedState)
                        ? new ShardedShopStateV2(serializedState)
                        : new ShardedShopStateV2(shopAddress);
                var orderDigest = order.Digest(avatarState, costumeStatSheet);
                shardedShopState.Add(orderDigest, context.BlockIndex);
                var orderReceiptList =
                    LegacyModule.TryGetState(world, orderReceiptAddress, out Dictionary receiptDict)
                        ? new OrderDigestListState(receiptDict)
                        : new OrderDigestListState(orderReceiptAddress);
                orderReceiptList.Add(orderDigest);

                world = LegacyModule.SetState(world, orderReceiptAddress, orderReceiptList.Serialize());
                world = LegacyModule.SetState(world, inventoryAddress, avatarState.inventory.Serialize());
                world = AvatarModule.SetAvatarStateV2(world, avatarAddress, avatarState);
                world = LegacyModule.SetState(world, itemAddress, tradableItem.Serialize());
                world = LegacyModule.SetState(world, orderAddress, order.Serialize());
                world = LegacyModule.SetState(world, shopAddress, shardedShopState.Serialize());
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

            return world;
        }
    }
}
