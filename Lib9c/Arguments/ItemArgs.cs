using System.Text.Json.Serialization;

namespace Lib9c.Arguments
{
    public interface IItemArgs
    {
        int? SheetId { get; }
        int? Count { get; }
        int? Level { get; }
        bool? Tradable { get; }
    }

    public struct ItemArgs : IItemArgs
    {
        public int? SheetId { get; }
        public int? Count { get; }
        public int? Level { get; }
        public bool? Tradable { get; }

        [JsonConstructor]
        public ItemArgs(
            int? sheetId = null,
            int? count = null,
            int? level = null,
            bool? tradable = null)
        {
            SheetId = sheetId;
            Count = count;
            Level = level;
            Tradable = tradable;
        }
    }
}
