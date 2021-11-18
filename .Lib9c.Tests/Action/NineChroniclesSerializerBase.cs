namespace Lib9c.Tests.Action
{
    using System;
    using Bencodex.Types;
    using Lib9c.Formatters;
    using Lib9c.Tests.Action;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using MessagePack;
    using MessagePack.Resolvers;
    using Nekoyume.Action;
    using Xunit;

    public class NineChroniclesSerializerBase
    {
        protected NineChroniclesSerializerBase()
        {
            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                NineChroniclesResolver.Instance,
                StandardResolver.Instance
            );
            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            MessagePackSerializer.DefaultOptions = options;
        }

        public void AssertAction(Type type, ActionBase action)
        {
            var currency = new Currency("NCG", 2, minters: null);
            var signer = default(Address);
            var blockIndex = 1234;
            var states = new State()
                .SetState(signer, (Text)"ANYTHING")
                .SetState(default, Dictionary.Empty.Add("key", "value"))
                .MintAsset(signer, currency * 10000);

            var evaluation = new ActionBase.ActionEvaluation<ActionBase>
            {
                Action = action,
                Signer = signer,
                BlockIndex = blockIndex,
                PreviousStates = states,
                OutputStates = states,
            };
            var serialize = MessagePackSerializer.Serialize(evaluation);
            var des = MessagePackSerializer.Deserialize<ActionBase.ActionEvaluation<ActionBase>>(serialize);

            Assert.IsType(type, des.Action);
            Assert.Equal(action.PlainValue, des.Action.PlainValue);
        }
    }
}
