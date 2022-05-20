using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Arena;
using Nekoyume.Extensions;
using Nekoyume.Model.Arena;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/
    /// </summary>
    [Serializable]
    [ActionType("reset_arena_ticket")]
    public class ResetArenaTicket : GameAction
    {
        public Address avatarAddress;
        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>()
            {
                ["avatarAddress"] = avatarAddress.Serialize(),
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
        }
        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            if (!states.TryGetAvatarStateV2(context.Signer, avatarAddress,
                    out var avatarState, out var _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(ArenaSheet),
                });

            var arenaSheet = sheets.GetSheet<ArenaSheet>();
            if (!ArenaHelper.TryGetOpenedRoundData(arenaSheet, context.BlockIndex, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"{nameof(ResetArenaTicket)} : block index({context.BlockIndex})");
            }

            var arenaInformationAdr = ArenaInformation.DeriveAddress(avatarAddress, roundData.Id, roundData.Round);
            if (!states.TryGetArenaInformation(arenaInformationAdr, out var arenaInformation))
            {
                throw new ArenaInformationNotFoundException(
                    $"[{nameof(ResetArenaTicket)}] my avatar address : {avatarAddress}" +
                    $" - ChampionshipId({roundData.Id}) - round({roundData.Round})");
            }

            var gameConfigState = states.GetGameConfigState();
            var interval = gameConfigState.DailyArenaInterval;
            var diff = context.BlockIndex - roundData.StartBlockIndex;
            var resetCount = (int)(diff / interval);

            if (arenaInformation.TicketResetCount >= resetCount)
            {
                throw new FailedToReachTicketResetBlockIndexException(
                    $"{nameof(ResetArenaTicket)} : " +
                    $"TicketResetCount({arenaInformation.TicketResetCount}) >= result({resetCount})");
            }

            arenaInformation.ResetTicket(resetCount);
            return states.SetState(arenaInformationAdr, arenaInformation.Serialize());
        }
    }
}
