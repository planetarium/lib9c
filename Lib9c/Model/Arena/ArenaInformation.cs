using Bencodex.Types;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Arena
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/1455
    /// </summary>
    public class ArenaInformation : IState
    {
        public static Address DeriveAddress(Address avatarAddress, int championshipId, int round) =>
            avatarAddress.Derive($"arena_information_{championshipId}_{round}");

        public const int MaxTicketCount = 8;

        public Address Address;
        public int Win { get; private set; }
        public int Lose { get; private set; }
        public int Ticket { get; private set; }
        public int TicketResetCount { get; private set; }
        public int PurchasedTicketCount { get; private set; }
        public int PurchasedTicketCountDuringResetInterval { get; private set; }

        public ArenaInformation(Address avatarAddress, int championshipId, int round)
        {
            Address = DeriveAddress(avatarAddress, championshipId, round);
            Ticket = MaxTicketCount;
        }

        public ArenaInformation(List serialized)
        {
            Address = serialized[0].ToAddress();
            Win = (Integer)serialized[1];
            Lose = (Integer)serialized[2];
            Ticket = (Integer)serialized[3];
            TicketResetCount = (Integer)serialized[4];
            PurchasedTicketCount = (Integer)serialized[5];
            if (serialized.Count > 6)
            {
                PurchasedTicketCountDuringResetInterval = (Integer) serialized[6];
            }
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(Address.Serialize())
                .Add(Win)
                .Add(Lose)
                .Add(Ticket)
                .Add(TicketResetCount)
                .Add(PurchasedTicketCount)
                .Add(PurchasedTicketCountDuringResetInterval);
        }

        public void UseTicket(int ticketCount)
        {
            if (Ticket < ticketCount)
            {
                throw new NotEnoughTicketException(
                    $"[{nameof(ArenaInformation)}] have({Ticket}) < use({ticketCount})");
            }

            Ticket -= ticketCount;
        }

        public void BuyTicket(ArenaSheet.RoundData roundData)
        {
            var max = roundData.MaxPurchaseCount;
            if (PurchasedTicketCount >= max)
            {
                throw new ExceedTicketPurchaseLimitException(
                    $"[{nameof(ArenaInformation)}] PurchasedTicketCount({PurchasedTicketCount}) >= MAX({max})");
            }

            var intervalMax = roundData.MaxPurchaseCountWithInterval;
            if (PurchasedTicketCountDuringResetInterval >= intervalMax)
            {
                throw new ExceedTicketPurchaseLimitDuringIntervalException(
                    $"[{nameof(ArenaInformation)}] PurchasedTicketCountDuringResetInterval({PurchasedTicketCountDuringResetInterval}) >= MAX({intervalMax})");
            }

            PurchasedTicketCount++;
            PurchasedTicketCountDuringResetInterval++;
        }

        public void UpdateRecord(int win, int lose)
        {
            Win += win;
            Lose += lose;
        }

        public void ResetTicket(int resetCount)
        {
            Ticket = MaxTicketCount;
            TicketResetCount = resetCount;
            PurchasedTicketCountDuringResetInterval = 0;
        }
    }
}
