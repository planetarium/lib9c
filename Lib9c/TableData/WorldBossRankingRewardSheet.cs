using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Types.Assets;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    // This sheet not on-chain data. don't call this sheet in `IAction.Execute()`
    public class WorldBossRankingRewardSheet : Sheet<int, WorldBossRankingRewardSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public struct RuneInfo
            {
                public int RuneId;
                public int RuneQty;

                public RuneInfo(int id, int qty)
                {
                    RuneId = id;
                    RuneQty = qty;
                }
            }

            public int Id;
            public int BossId;
            public int RankingMin;
            public int RankingMax;
            public int RateMin;
            public int RateMax;
            public List<RuneInfo> Runes;
            public int Crystal;
            public List<(int itemId, int quantity)> Materials;
            public override int Key => Id;
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                BossId = ParseInt(fields[1]);
                RankingMin = ParseInt(fields[2]);
                RankingMax = ParseInt(fields[3]);
                RateMin = ParseInt(fields[4]);
                RateMax = ParseInt(fields[5]);
                Runes = new List<RuneInfo>();
                for (int i = 0; i < 3; i++)
                {
                    var offset = i * 2;
                    Runes.Add(new RuneInfo(ParseInt(fields[6 + offset]), ParseInt(fields[7 + offset])));
                }
                Crystal = ParseInt(fields[12]);

                if (fields.Count > 13)
                {
                    Materials = new List<(int, int)>();
                    for (int i = 0; i < 2; i++)
                    {
                        var offset = i * 2;
                        Materials.Add(
                            (ParseInt(fields[13 + offset]), ParseInt(fields[14 + offset])));
                    }
                }
            }

            public (List<FungibleAssetValue> assets, Dictionary<TradableMaterial, int> materials) GetRewards(
                RuneSheet runeSheet,
                MaterialItemSheet materialSheet)
            {
                var assets = new List<FungibleAssetValue>
                {
                    Crystal * CrystalCalculator.CRYSTAL
                };
                assets.AddRange(Runes
                    .Where(runeInfo => runeInfo.RuneQty > 0)
                    .Select(runeInfo =>
                        RuneHelper.ToFungibleAssetValue(runeSheet[runeInfo.RuneId],
                            runeInfo.RuneQty)));

                var materials = new Dictionary<TradableMaterial, int>();
                foreach (var (itemId, quantity) in Materials)
                {
                    var materialRow = materialSheet.Values.First(r => r.Id == itemId);
                    var material = ItemFactory.CreateTradableMaterial(materialRow);
                    materials.TryAdd(material, 0);
                    materials[material] += quantity;
                }
                return (assets, materials);
            }
        }

        public WorldBossRankingRewardSheet() : base(nameof(WorldBossRankingRewardSheet))
        {
        }

        public Row FindRow(int bossId, int ranking, int rate)
        {
            if (ranking <= 0 && rate <= 0)
            {
                throw new ArgumentException($"ranking or rate must be greater than 0. ranking: {ranking}, rate: {rate}");
            }
            return OrderedList.LastOrDefault(r => r.BossId == bossId && r.RankingMin <= ranking && ranking <= r.RankingMax) ?? OrderedList.LastOrDefault(r => r.BossId == bossId && r.RateMin <= rate && rate <= r.RateMax);
        }
    }
}
