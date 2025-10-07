using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Bencodex.Types;
using Lib9c.Abstractions;
using Lib9c.Extensions;
using Lib9c.Model.Item;
using Lib9c.Model.State;
using Lib9c.Module;
using Lib9c.TableData;
using Lib9c.TableData.Item;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Serilog;
#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
using Lib9c.DevExtensions.Manager.Contents;
#endif

namespace Lib9c.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/2195
    /// </summary>
    [Serializable]
    [ActionType("create_avatar11")]
    public class CreateAvatar : GameAction, ICreateAvatarV2
    {
        public const string DeriveFormat = "avatar-state-{0}";

        public int index;
        public int hair;
        public int lens;
        public int ear;
        public int tail;
        public string name;

        int ICreateAvatarV2.Index => index;
        int ICreateAvatarV2.Hair => hair;
        int ICreateAvatarV2.Lens => lens;
        int ICreateAvatarV2.Ear => ear;
        int ICreateAvatarV2.Tail => tail;
        string ICreateAvatarV2.Name => name;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>()
            {
                ["index"] = (Integer)index,
                ["hair"] = (Integer)hair,
                ["lens"] = (Integer)lens,
                ["ear"] = (Integer)ear,
                ["tail"] = (Integer)tail,
                ["name"] = (Text)name,
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            index = (int)((Integer)plainValue["index"]).Value;
            hair = (int)((Integer)plainValue["hair"]).Value;
            lens = (int)((Integer)plainValue["lens"]).Value;
            ear = (int)((Integer)plainValue["ear"]).Value;
            tail = (int)((Integer)plainValue["tail"]).Value;
            name = (Text)plainValue["name"];
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);
            var ctx = context;
            var signer = ctx.Signer;
            var states = ctx.PreviousState;
            var avatarAddress = signer.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DeriveFormat,
                    index
                )
            );

            var random = ctx.GetRandom();
            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            ValidateName(addressesHex);

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}CreateAvatar exec started", addressesHex);

            var agentState = GetAgentState(states, signer, avatarAddress, addressesHex);

            sw.Stop();
            Log.Verbose("{AddressesHex}CreateAvatar Get AgentAvatarStates: {Elapsed}", addressesHex, sw.Elapsed);
            sw.Restart();

            Log.Verbose("{AddressesHex}Execute CreateAvatar; player: {AvatarAddress}", addressesHex, avatarAddress);

            agentState.avatarAddresses.Add(index, avatarAddress);

            // Avoid NullReferenceException in test
            var materialItemSheet = ctx.PreviousState.GetSheet<MaterialItemSheet>();
            var avatarState = CreateAvatarState(name, avatarAddress, ctx, default);

            CustomizeAvatar(avatarState);

            var allCombinationSlotState = CreateCombinationSlots(avatarAddress);
            states = states.SetCombinationSlotState(avatarAddress, allCombinationSlotState);

            avatarState.UpdateQuestRewards(materialItemSheet);

#if LIB9C_DEV_EXTENSIONS || UNITY_EDITOR
            states = CreateAvatarManager.ExecuteDevExtensions(ctx, avatarAddress, states, avatarState, random);
#endif

            var sheets = ctx.PreviousState.GetSheets(containItemSheet: true,
                sheetTypes: new[]
                {
                    typeof(CreateAvatarItemSheet), typeof(CreateAvatarFavSheet),
                });
            var itemSheet = sheets.GetItemSheet();
            var createAvatarItemSheet = sheets.GetSheet<CreateAvatarItemSheet>();
            AddItem(itemSheet, createAvatarItemSheet, avatarState, random);
            var createAvatarFavSheet = sheets.GetSheet<CreateAvatarFavSheet>();
            states = MintAsset(createAvatarFavSheet, avatarState, states, context);

            sw.Stop();
            Log.Verbose("{AddressesHex}CreateAvatar CreateAvatarState: {Elapsed}", addressesHex, sw.Elapsed);
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}CreateAvatar Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetAgentState(signer, agentState)
                .SetAvatarState(avatarAddress, avatarState)
                .SetActionPoint(avatarAddress, DailyReward.ActionPointMax)
                .SetDailyRewardReceivedBlockIndex(avatarAddress, 0L);
        }

        private void ValidateName(string addressesHex)
        {
            if (!Regex.IsMatch(name, GameConfig.AvatarNickNamePattern))
            {
                throw new InvalidNamePatternException(
                    $"{addressesHex}Aborted as the input name {name} does not follow the allowed name pattern.");
            }
        }

        private AgentState GetAgentState(IWorld states, Address signer, Address avatarAddress, string addressesHex)
        {
            var existingAgentState = states.GetAgentState(signer);
            var agentState = existingAgentState ?? new AgentState(signer);
            // check has avatar in avatarAddress, see InvalidAddressException in this method
            var avatarState = states.GetAvatarState(avatarAddress, false, false, false);
            if (avatarState is not null)
            {
                throw new InvalidAddressException(
                    $"{addressesHex}Aborted as there is already an avatar at {avatarAddress}.");
            }

            if (index is < 0 or >= GameConfig.SlotCount)
            {
                throw new AvatarIndexOutOfRangeException(
                    $"{addressesHex}Aborted as the index is out of range #{index}.");
            }

            if (agentState.avatarAddresses.ContainsKey(index))
            {
                throw new AvatarIndexAlreadyUsedException(
                    $"{addressesHex}Aborted as the signer already has an avatar at index #{index}.");
            }

            return agentState;
        }

        private void CustomizeAvatar(AvatarState avatarState)
        {
            if (hair < 0)
            {
                hair = 0;
            }

            if (lens < 0)
            {
                lens = 0;
            }

            if (ear < 0)
            {
                ear = 0;
            }

            if (tail < 0)
            {
                tail = 0;
            }

            avatarState.Customize(hair, lens, ear, tail);
        }

        private AllCombinationSlotState CreateCombinationSlots(Address avatarAddress)
        {
            var allCombinationSlotState = new AllCombinationSlotState();
            for (var i = 0; i < AvatarState.DefaultCombinationSlotCount; i++)
            {
                var slotAddr = Addresses.GetCombinationSlotAddress(avatarAddress, i);
                var slot = new CombinationSlotState(slotAddr, i);
                allCombinationSlotState.AddSlot(slot);
            }

            return allCombinationSlotState;
        }

        public static void AddItem(ItemSheet itemSheet, CreateAvatarItemSheet createAvatarItemSheet,
            AvatarState avatarState, IRandom random)
        {
            foreach (var row in createAvatarItemSheet.Values)
            {
                var itemId = row.ItemId;
                var count = row.Count;
                var itemRow = itemSheet[itemId];
                if (itemRow is MaterialItemSheet.Row materialRow)
                {
                    var item = ItemFactory.CreateMaterial(materialRow);
                    avatarState.inventory.AddItem(item, count);
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        var item = ItemFactory.CreateItem(itemRow, random);
                        avatarState.inventory.AddItem(item);
                    }
                }
            }
        }

        public static IWorld MintAsset(CreateAvatarFavSheet favSheet,
            AvatarState avatarState, IWorld states, IActionContext context)
        {
            foreach (var row in favSheet.Values)
            {
                var currency = row.Currency;
                var targetAddress = row.Target switch
                {
                    CreateAvatarFavSheet.Target.Agent => avatarState.agentAddress,
                    CreateAvatarFavSheet.Target.Avatar => avatarState.address,
                    _ => throw new ArgumentOutOfRangeException(),
                };
                states = states.MintAsset(context, targetAddress, currency * row.Quantity);
            }

            return states;
        }

        public static AvatarState CreateAvatarState(string name,
            Address avatarAddress,
            IActionContext ctx,
            Address rankingMapAddress)
        {
            var state = ctx.PreviousState;
            var avatarState = AvatarState.Create(
                avatarAddress,
                ctx.Signer,
                ctx.BlockIndex,
                state.GetAvatarSheets(),
                rankingMapAddress,
                name
            );

            return avatarState;
        }
    }
}
