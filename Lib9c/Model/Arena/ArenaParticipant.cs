using System;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Exceptions;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Arena
{
    /// <summary>
    /// This class combines the information from <see cref="Nekoyume.Model.Arena.ArenaScore"/> and
    /// <see cref="Nekoyume.Model.Arena.ArenaInformation"/>, and brings together information about the arena
    /// participant, including additional information.
    /// </summary>
    public class ArenaParticipant : IBencodable, IState
    {
        private const int StateVersion = 1;

        public const int DefaultScore = 1000;
        public const int MaxTicketCount = 8;

        public readonly Address AvatarAddr;

        /// <summary>
        /// If you need to know <see cref="Nekoyume.Model.State.AvatarState.NameWithHash"/>, check
        /// <see cref="Nekoyume.Model.State.AvatarState.PostConstructor"/> method of the
        /// <see cref="Nekoyume.Model.State.AvatarState"/> class and you can find the relevant information there. It
        /// provides a formatted string that includes the avatar's <see cref="Nekoyume.Model.State.AvatarState.name"/>
        /// and a shortened version of their address.
        /// </summary>
        /// <example>
        /// <code>
        /// $"{name} &lt;size=80%&gt;&lt;color=#A68F7E&gt;#{address.ToHex().Substring(0, 4)}&lt;/color&gt;&lt;/size&gt;";
        /// </code>
        /// </example>
        public string Name;

        public int PortraitId;
        public int Level;
        public long Cp;

        public int Score;

        public int Ticket;
        public int TicketResetCount;
        public int PurchasedTicketCount;

        public int Win;
        public int Lose;

        public long LastBattleBlockIndex;

        public IValue Bencoded => List.Empty
            .Add(StateVersion)
            .Add(AvatarAddr.Serialize())
            .Add(Name)
            .Add(PortraitId)
            .Add(Level)
            .Add(Cp)
            .Add(Score)
            .Add(Ticket)
            .Add(TicketResetCount)
            .Add(PurchasedTicketCount)
            .Add(Win)
            .Add(Lose)
            .Add(LastBattleBlockIndex);

        public ArenaParticipant(Address avatarAddr)
        {
            AvatarAddr = avatarAddr;
            Score = DefaultScore;
            Ticket = MaxTicketCount;
        }

        public ArenaParticipant(IValue bencoded)
        {
            if (bencoded is not List l)
            {
                throw new ArgumentException($"Invalid bencoded value: {bencoded.Inspect()}", nameof(bencoded));
            }

            try
            {
                var stateVersion = (Integer)l[0];
                if (stateVersion != StateVersion)
                {
                    throw new UnsupportedStateException(StateVersion, stateVersion);
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException("Invalid state type name or version", e);
            }

            AvatarAddr = l[1].ToAddress();
            Name = (Text)l[2];
            PortraitId = (Integer)l[3];
            Level = (Integer)l[4];
            Cp = (Integer)l[5];
            Score = (Integer)l[6];
            Ticket = (Integer)l[7];
            TicketResetCount = (Integer)l[8];
            PurchasedTicketCount = (Integer)l[9];
            Win = (Integer)l[10];
            Lose = (Integer)l[11];
            LastBattleBlockIndex = (Integer)l[12];
        }

        public IValue Serialize() => Bencoded;

        public void AddScore(int score)
        {
            Score = Math.Max(Score + score, DefaultScore);
        }

        public void UseTicket(int ticketCount)
        {
            if (Ticket < ticketCount)
            {
                throw new NotEnoughTicketException(
                    $"[{nameof(ArenaParticipant)}] have({Ticket}) < use({ticketCount})");
            }

            Ticket -= ticketCount;
        }

        public void BuyTicket(long maxCount)
        {
            if (PurchasedTicketCount >= maxCount)
            {
                throw new ExceedTicketPurchaseLimitException(
                    $"[{nameof(ArenaParticipant)}] PurchasedTicketCount({PurchasedTicketCount}) >= MAX({maxCount})");
            }

            PurchasedTicketCount++;
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
        }
    }
}
