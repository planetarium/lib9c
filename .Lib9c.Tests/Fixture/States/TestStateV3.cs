namespace Lib9c.Tests.Fixture.States
{
    using Bencodex.Types;
    using Nekoyume.Model;
    using Nekoyume.Model.State;

    // Remove `int Value` and Add `ITestStateV1 Age` from `ITestStateV2`.
    [VersionedState(Moniker, Version)]
    // [VersionedStateType(typeof(List))]
    public interface ITestStateV3
    {
        public const string Moniker = "test";
        public const uint Version = 3;

        // [VersionedStateListElement(0)]
        public Text Name { get; }

        // [VersionedStateListElement(1)]
        public ITestStateV1 Age { get; }
    }

    [VersionedStateImpl(typeof(ITestStateV3))]
    public class TestStateV3 : IState
    {
        public string Name { get; }

        public TestStateV1 Age { get; }

        public TestStateV3() : this(string.Empty, 0)
        {
        }

        public TestStateV3(string name, int age)
        {
            Name = name;
            Age = new TestStateV1(age);
        }

        public TestStateV3(IValue serialized)
        {
            var list = (List)serialized;
            Name = (Text)list[0];
            Age = new TestStateV1(list[1]);
        }

        public IValue Serialize() => new List(
            (Text)Name,
            Age.Serialize());
    }
}
