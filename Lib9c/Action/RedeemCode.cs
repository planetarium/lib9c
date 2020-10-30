using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("redeem_code")]
    public class RedeemCode : GameAction
    {
        public string Code { get; internal set; }

        public Address AvatarAddress {get; internal set; }

        public RedeemCode()
        {
        }

        public RedeemCode(string code, Address avatarAddress)
        {
            Code = code;
            AvatarAddress = avatarAddress;
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                states = states.SetState(RedeemCodeState.Address, MarkChanged);
                states = states.SetState(AvatarAddress, MarkChanged);
                states = states.SetState(context.Signer, MarkChanged);
                states = states.MarkBalanceChanged(GoldCurrencyMock, GoldCurrencyState.Address);
                states = states.MarkBalanceChanged(GoldCurrencyMock, context.Signer);
                return states;
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("RedeemCode exec started.");

            if (!states.TryGetAgentAvatarStates(context.Signer, AvatarAddress, out AgentState agentState,
                out AvatarState avatarState))
            {
                return states;
            }

            sw.Stop();
            Log.Debug("RedeemCode Get AgentAvatarStates: {Elapsed}", sw.Elapsed);
            sw.Restart();

            var redeemState = states.GetRedeemCodeState();
            if (redeemState is null)
            {
                return states;
            }

            sw.Stop();
            Log.Debug("RedeemCode Get RedeemCodeState: {Elapsed}", sw.Elapsed);
            sw.Restart();

            int redeemId;
            try
            {
                redeemId = redeemState.Redeem(Code, AvatarAddress);
            }
            catch (InvalidRedeemCodeException)
            {
                Log.Error("Invalid Code");
                throw;
            }
            catch (DuplicateRedeemException e)
            {
                Log.Warning(e.Message);
                throw;
            }

            sw.Stop();
            Log.Debug("RedeemCode Redeem(): {Elapsed}", sw.Elapsed);
            sw.Restart();

            var row = states.GetSheet<RedeemRewardSheet>().Values.First(r => r.Id == redeemId);
            var itemSheets = states.GetItemSheet();

            foreach (RedeemRewardSheet.RewardInfo info in row.Rewards)
            {
                switch (info.Type)
                {
                    case RewardType.Item:
                        for (var i = 0; i < info.Quantity; i++)
                        {
                            if (info.ItemId is int itemId)
                            {
                                ItemBase item = ItemFactory.CreateItem(itemSheets[itemId], context.Random);
                                // We should fix count as 1 because ItemFactory.CreateItem
                                // will create a new item every time.
                                avatarState.inventory.AddItem(item, 1);
                            }
                        }
                        states = states.SetState(AvatarAddress, avatarState.Serialize());
                        break;
                    case RewardType.Gold:
                        states = states.TransferAsset(
                            GoldCurrencyState.Address,
                            context.Signer,
                            states.GetGoldCurrency() * info.Quantity
                        );
                        break;
                    default:
                        // FIXME: We should raise exception here.
                        break;
                }
                sw.Stop();
                Log.Debug($"RedeemCode Get Reward {info.Type}: {sw.Elapsed}");
                sw.Restart();
            }
            states = states.SetState(RedeemCodeState.Address, redeemState.Serialize());
            sw.Stop();
            Log.Debug("RedeemCode Serialize RedeemCodeState: {Elapsed}", sw.Elapsed);
            sw.Restart();
            states = states.SetState(context.Signer, agentState.Serialize());
            sw.Stop();
            Log.Debug("RedeemCode Serialize AvatarState: {Elapsed}", sw.Elapsed);

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("RedeemCode Total Executed Time: {Elapsed}", ended - started);
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [nameof(Code)] = Code.Serialize(),
                [nameof(AvatarAddress)] = AvatarAddress.Serialize()
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            Code = (Text) plainValue[nameof(Code)];
            AvatarAddress = plainValue[nameof(AvatarAddress)].ToAddress();
        }
    }
}
