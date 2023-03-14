using Bencodex;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using Nekoyume.Model;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("delay_action")]
    public class DelayAction : ActionBase, ISerializable
    {
        protected DelayAction(SerializationInfo info, StreamingContext context)
        {
            var rawBytes = (byte[])info.GetValue("serialized", typeof(byte[]));
            Dictionary pv = (Dictionary) new Codec().Decode(rawBytes);

            LoadPlainValue(pv);
        }

        public DelayAction()
        {
        }

        public int MilliSecond { get; private set; }

        public override IValue PlainValue
        {
            get
            {
                IEnumerable<KeyValuePair<IKey, IValue>> pairs = new[]
                {
                    new KeyValuePair<IKey, IValue>((Text)"m", MilliSecond.Serialize()),
                };

                return new Dictionary(pairs);
            }
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var state = context.PreviousStates;
            var started = DateTimeOffset.UtcNow;
            Log.Debug(
                "{MethodName} exec started. Delay target: {MilliSecond}",
                nameof(DelayAction),
                MilliSecond);
            Thread.Sleep(MilliSecond);
            var ended = DateTimeOffset.UtcNow;
            Log.Debug(
                "{MethodName} Total Executed Time: {Elapsed}. Delay target: {MilliSecond}",
                nameof(DelayAction),
                ended - started,
                MilliSecond);
            return state;
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary)plainValue;
            MilliSecond = asDict["m"].ToInteger();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("serialized", new Codec().Encode(PlainValue));
        }
    }
}
