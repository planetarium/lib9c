namespace Lib9c.Tests.Fixture.States
{
    using Bencodex.Types;
    using Nekoyume.Model;
    using Nekoyume.Model.State;

    // Expand `string Name` from `ITestStateV1`.
    [VersionedState(Moniker, Version)]
    // [VersionedStateType(typeof(List))]
    public interface ITestStateV2
    {
        public const string Moniker = "test";
        public const uint Version = 2;

        // [VersionedStateListElement(0)]
        public Integer Value { get; }

        // [VersionedStateListElement(1)]
        public Text Name { get; }
    }

    [VersionedStateImpl(typeof(ITestStateV2))]
    public class TestStateV2 : IState
    {
        public int Value { get; }

        public string Name { get; }

        public TestStateV2() : this(0, string.Empty)
        {
        }

        public TestStateV2(int value, string name)
        {
            Value = value;
            Name = name;
        }

        public TestStateV2(IValue serialized)
        {
            var list = (List)serialized;
            Value = (Integer)list[0];
            Name = (Text)list[1];
        }

        public IValue Serialize() => new List(
            (Integer)Value,
            (Text)Name);
    }
}
