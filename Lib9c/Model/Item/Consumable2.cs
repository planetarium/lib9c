using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Consumable2 : ItemUsable2
    {
        public StatType MainStat => Stats.Any() ? Stats[0].StatType : StatType.NONE;

        // FIXME: remove
        public List<StatMap> Stats { get; }

        public Consumable2(
            int serializedVersion,
            ConsumableItemSheet.Row data,
            Guid id,
            long requiredBlockIndex,
            int requiredCharacterLevel)
            : base(serializedVersion, data, id, requiredBlockIndex, requiredCharacterLevel)
        {
            Stats = data.Stats;
        }

        public Consumable2(Dictionary serialized) : base(serialized)
        {
            if (serialized.TryGetValue((Text) "stats", out var stats))
            {
                Stats = stats.ToList(i => new StatMap((Dictionary) i));
            }
            
            UpdateBaseOptionAndOtherOptions(
                MainStat,
                StatsMap,
                Skills,
                BuffSkills);
        }
        
        protected Consumable2(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "stats"] = new List(Stats
                    .OrderBy(i => i.StatType)
                    .ThenByDescending(i => i.Value)
                    .Select(s => s.Serialize())),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
    }
}
