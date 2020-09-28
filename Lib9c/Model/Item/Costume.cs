using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Costume : ItemBase
    {
        public bool equipped = false;
        public string SpineResourcePath { get; }
        public StatsMap StatsMap { get; }

        public Costume(CostumeItemSheet.Row data) : base(data)
        {
            SpineResourcePath = data.SpineResourcePath;
            StatsMap = new StatsMap();
        }

        public Costume(Dictionary serialized) : base(serialized)
        {
            StatsMap = new StatsMap();
            if (serialized.TryGetValue((Text) "equipped", out var toEquipped))
            {
                equipped = toEquipped.ToBoolean();
            }
            if (serialized.TryGetValue((Text) "spine_resource_path", out var spineResourcePath))
            {
                SpineResourcePath = (Text) spineResourcePath;
            }
            if (serialized.TryGetValue((Text) "stats_map", out var statsMap))
            {
                StatsMap.Deserialize((Dictionary)statsMap);
            }
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "equipped"] = equipped.Serialize(),
                [(Text) "spine_resource_path"] = SpineResourcePath.Serialize(),
                [(Text) "stats_map"] = StatsMap.Serialize(),
            }.Union((Dictionary) base.Serialize()));
    }
}
