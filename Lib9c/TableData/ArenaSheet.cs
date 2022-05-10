using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.TableData
{
    using static TableExtensions;

    [Serializable]
    public class ArenaSheet : Sheet<int, ArenaSheet.Row>
    {
        public class RoundData
        {
            public int Id { get; }
            public int Round { get; }
            public int ArenaType { get; }
            public long StartIndex { get; }
            public long EndIndex { get; }
            public int RequiredWins { get; }
            public long EntranceFee { get; }
            public decimal TicketPrice { get; }
            public decimal AdditionalTicketPrice { get; }

            public RoundData(int id, int round, int arenaType,
                long startIndex, long endIndex,
                int requiredWins, long entranceFee,
                decimal ticketPrice, decimal additionalTicketPrice)
            {
                Id = id;
                Round = round;
                ArenaType = arenaType;
                StartIndex = startIndex;
                EndIndex = endIndex;
                RequiredWins = requiredWins;
                EntranceFee = entranceFee;
                TicketPrice = ticketPrice;
                AdditionalTicketPrice = additionalTicketPrice;
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public List<RoundData> Round { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                var round = ParseInt(fields[1]);
                var arenaType = ParseInt(fields[2]);
                var startIndex = ParseLong(fields[3]);
                var endIndex = ParseLong(fields[4]);
                var requiredWins = ParseInt(fields[5]);
                var entranceFee = ParseLong(fields[6]);
                var ticketPrice = ParseDecimal(fields[7]);
                var additionalTicketPrice = ParseDecimal(fields[8]);
                Round = new List<RoundData>
                {
                    new RoundData(Id, round, arenaType, startIndex, endIndex,
                        requiredWins, entranceFee,
                        ticketPrice, additionalTicketPrice)
                };
            }

            public bool IsIn(long blockIndex)
            {
                return Round.Exists(x =>
                    x.StartIndex <= blockIndex && blockIndex < x.EndIndex);
            }

            public bool TryGetRound(long blockIndex, out RoundData roundData)
            {
                roundData = Round.FirstOrDefault(x =>
                    x.StartIndex <= blockIndex && blockIndex < x.EndIndex);
                return !(roundData is null);
            }
        }

        public ArenaSheet() : base(nameof(ArenaSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (!value.Round.Any())
            {
                return;
            }

            row.Round.Add(value.Round[0]);
        }
    }
}
