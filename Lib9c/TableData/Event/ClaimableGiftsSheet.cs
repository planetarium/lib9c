using System.Collections.Generic;
using System.Linq;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    /// <summary>
    /// Sheet for claimable gifts that can be claimed by players during specific block index ranges.
    /// Each row contains gift ID, block index range, and a list of items to be given.
    /// </summary>
    public class ClaimableGiftsSheet : Sheet<int, ClaimableGiftsSheet.Row>
    {
        /// <summary>
        /// Represents a single row in the ClaimableGiftsSheet.
        /// Contains gift ID, block index range, and items to be given.
        /// </summary>
        public class Row : SheetRow<int>
        {
            /// <summary>
            /// Gets the key for this row, which is the gift ID.
            /// </summary>
            public override int Key => Id;

            /// <summary>
            /// Gift ID that uniquely identifies this gift.
            /// </summary>
            public int Id;

            /// <summary>
            /// Block index when this gift becomes claimable (inclusive).
            /// </summary>
            public long StartedBlockIndex;

            /// <summary>
            /// Block index when this gift becomes unclaimable (inclusive).
            /// </summary>
            public long EndedBlockIndex;

            /// <summary>
            /// List of items to be given when this gift is claimed.
            /// Each tuple contains (itemId, quantity, tradable).
            /// </summary>
            public List<(int itemId, int quantity, bool tradable)> Items;

            /// <summary>
            /// Sets the row data from CSV fields.
            /// Expected format: id,started_block_index,ended_block_index,item_1_id,item_1_quantity,item_1_tradable,...,item_5_id,item_5_quantity,item_5_tradable
            /// Supports up to 5 items. Empty or zero values for item_id or quantity will be skipped.
            /// </summary>
            /// <param name="fields">CSV field values</param>
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                StartedBlockIndex = ParseLong(fields[1]);
                EndedBlockIndex = ParseLong(fields[2]);

                Items = new List<(int, int, bool)>();
                for (int i = 0; i < 5; i++)
                {
                    var offset = i * 3;
                    var itemIdIndex = 3 + offset;
                    var quantityIndex = 4 + offset;
                    var tradableIndex = 5 + offset;

                    if (fields.Count <= tradableIndex)
                    {
                        break;
                    }

                    if (!TryParseInt(fields[itemIdIndex], out var materialId) || materialId == 0 ||
                        !TryParseInt(fields[quantityIndex], out var quantity) || quantity == 0)
                    {
                        continue;
                    }

                    var tradable = ParseBool(fields[tradableIndex], true);
                    Items.Add((materialId, quantity, tradable));
                }
            }

            /// <summary>
            /// Validates whether the given block index is within the claimable range.
            /// </summary>
            /// <param name="blockIndex">Block index to validate</param>
            /// <returns>True if the block index is within [StartedBlockIndex, EndedBlockIndex], false otherwise</returns>
            public bool Validate(long blockIndex)
            {
                return StartedBlockIndex <= blockIndex && blockIndex <= EndedBlockIndex;
            }
        }

        /// <summary>
        /// Initializes a new instance of the ClaimableGiftsSheet class.
        /// </summary>
        public ClaimableGiftsSheet() : base(nameof(ClaimableGiftsSheet))
        {
        }

        /// <summary>
        /// Tries to find a row that is claimable at the given block index.
        /// </summary>
        /// <param name="blockIndex">Block index to search for</param>
        /// <param name="row">When this method returns, contains the row if found; otherwise, null</param>
        /// <returns>True if a claimable row is found; otherwise, false</returns>
        public bool TryFindRowByBlockIndex(long blockIndex, out Row row)
        {
            row = OrderedList.FirstOrDefault(r => r.StartedBlockIndex <= blockIndex && blockIndex <= r.EndedBlockIndex);
            return row != null;
        }
    }
}
