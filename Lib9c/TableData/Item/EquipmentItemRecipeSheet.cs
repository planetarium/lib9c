using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    /// <summary>
    /// Equipment crafting recipes.
    /// </summary>
    public class EquipmentItemRecipeSheet : Sheet<int, EquipmentItemRecipeSheet.Row>
    {
        /// <summary>
        /// One equipment recipe.
        /// </summary>
        public class Row : SheetRow<int>
        {
            /// <inheritdoc />
            public override int Key => Id;
            /// <summary>
            /// The recipe id.
            /// </summary>
            public int Id { get; private set; }
            /// <summary>
            /// The equipment the recipe makes.
            /// </summary>
            public int ResultEquipmentId { get; private set; }
            /// <summary>
            /// The main material.
            /// </summary>
            public int MaterialId { get; private set; }
            /// <summary>
            /// How many of the main material are used.
            /// </summary>
            public int MaterialCount { get; private set; }
            /// <summary>
            /// Action points the craft costs.
            /// </summary>
            public int RequiredActionPoint { get; private set; }
            /// <summary>
            /// NCG the craft costs.
            /// </summary>
            public long RequiredGold { get; private set; }
            /// <summary>
            /// Blocks the craft takes.
            /// </summary>
            public long RequiredBlockIndex { get; private set; }
            /// <summary>
            /// The stage that must be cleared to unlock the recipe.
            /// </summary>
            public int UnlockStage { get; private set; }
            /// <summary>
            /// The sub recipes of this recipe.
            /// </summary>
            public List<int> SubRecipeIds { get; private set; }
            /// <summary>
            /// Crystal to unlock the recipe (column <c>required_crystal</c>), 0 for a recipe open from the start.
            /// </summary>
            public long CRYSTAL { get; private set; }
            /// <summary>
            /// The result equipment's sub type, when given.
            /// </summary>
            public ItemSubType? ItemSubType { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                ResultEquipmentId = ParseInt(fields[1]);
                MaterialId = ParseInt(fields[2]);
                MaterialCount = ParseInt(fields[3]);
                RequiredActionPoint = ParseInt(fields[4]);
                RequiredGold = ParseLong(fields[5]);
                RequiredBlockIndex = ParseLong(fields[6]);
                UnlockStage = ParseInt(fields[7]);
                SubRecipeIds = new List<int>(3);
                for (var i = 8; i < 11; i++)
                {
                    if (string.IsNullOrEmpty(fields[i]))
                    {
                        break;
                    }
                    SubRecipeIds.Add(ParseInt(fields[i]));
                }

                CRYSTAL = 0;
                if (fields.Count >= 12)
                {
                    var currentField = fields[11];
                    CRYSTAL = string.IsNullOrEmpty(currentField) ? 0 : ParseLong(currentField);
                }

                if (fields.Count >= 13)
                {
                    ItemSubType = (ItemSubType) Enum.Parse(typeof(ItemSubType), fields[12]);
                }
            }

            [Obsolete("It is obsoleted. Check EquipmentItemSubRecipeSheetV2.IsMimisbrunnrSubRecipe.")]
            public bool IsMimisBrunnrSubRecipe(int? subRecipeId)
            {
                return subRecipeId.HasValue && SubRecipeIds[2] == subRecipeId.Value;
            }

            public IEnumerable<EquipmentItemSubRecipeSheet.MaterialInfo> GetAllMaterials(
                EquipmentItemSubRecipeSheetV2 sheet,
                CraftType subRecipeType = CraftType.Normal
            )
            {
                var allMaterials =
                    new List<EquipmentItemSubRecipeSheet.MaterialInfo>
                    {
                        new EquipmentItemSubRecipeSheet.MaterialInfo(MaterialId, MaterialCount)
                    };

                var subRecipeList =
                    sheet.OrderedList.Where(e => SubRecipeIds.Contains(e.Id)).ToList();
                allMaterials = subRecipeType switch
                {
                    CraftType.Normal => allMaterials.Concat(subRecipeList[0].Materials).ToList(),
                    CraftType.Premium => allMaterials.Concat(subRecipeList[1].Materials).ToList(),
                    CraftType.Mimisbrunnr => allMaterials.Concat(subRecipeList[2].Materials)
                        .ToList(),
                    _ => allMaterials
                };

                return allMaterials;
            }
        }

        /// <summary>
        /// Creates an empty sheet.
        /// </summary>
        public EquipmentItemRecipeSheet() : base(nameof(EquipmentItemRecipeSheet))
        {
        }
    }
}
