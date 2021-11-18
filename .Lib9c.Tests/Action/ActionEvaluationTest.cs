namespace Lib9c.Tests.Action
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using MessagePack;
    using Nekoyume.Action;
    using Xunit;

    [Collection("Resolver Collection")]
    public class ActionEvaluationTest
    {
        private readonly Currency _currency;
        private readonly Address _signer;
        private readonly ActionBase.ActionEvaluation<ActionBase> _evaluation;

        public ActionEvaluationTest()
        {
            _currency = new Currency("NCG", 2, minters: null);
            var blockIndex = 1234;
            _signer = new PrivateKey().ToAddress();
            var sender = new PrivateKey().ToAddress();
            var states = new State()
                .SetState(_signer, (Text)"ANYTHING")
                .SetState(default, Dictionary.Empty.Add("key", "value"))
                .MintAsset(_signer, _currency * 10000);
            var action = new TransferAsset(
                sender: _signer,
                recipient: sender,
                amount: _currency * 100
            );

            _evaluation = new ActionBase.ActionEvaluation<ActionBase>()
            {
                Action = action,
                Signer = _signer,
                BlockIndex = blockIndex,
                PreviousStates = states,
                OutputStates = states,
            };
        }

        [Fact]
        public void Serialize_With_DotnetAPI()
        {
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, _evaluation);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (ActionBase.ActionEvaluation<ActionBase>)formatter.Deserialize(ms);

            // FIXME We should equality check more precisely.
            Assert.Equal(_evaluation.Signer, deserialized.Signer);
            Assert.Equal(_evaluation.BlockIndex, deserialized.BlockIndex);
            var dict = (Dictionary)deserialized.OutputStates.GetState(default)!;
            Assert.Equal("value", (Text)dict["key"]);
            Assert.Equal(_currency * 10000, deserialized.OutputStates.GetBalance(_signer, _currency));
        }

        [Fact]
        public void Serialize_With_MessagePack()
        {
            var b = MessagePackSerializer.Serialize(_evaluation);
            var deserialized = MessagePackSerializer.Deserialize<ActionBase.ActionEvaluation<ActionBase>>(b);
            // FIXME We should equality check more precisely.
            Assert.Equal(_evaluation.Signer, deserialized.Signer);
            Assert.Equal(_evaluation.BlockIndex, deserialized.BlockIndex);
            var dict = (Dictionary)deserialized.OutputStates.GetState(default)!;
            Assert.Equal("value", (Text)dict["key"]);
            Assert.Equal(_currency * 10000, deserialized.OutputStates.GetBalance(_signer, _currency));
        }
    }
}
