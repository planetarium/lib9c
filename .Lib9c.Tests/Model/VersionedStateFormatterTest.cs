namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Lib9c.Tests.Fixture.States;
    using Nekoyume.Model;
    using Xunit;

    public class VersionedStateFormatterTest
    {
        public static IEnumerable<object[]> GetMemberData_For_TestStateV1_InvalidSerializedValue()
        {
            yield return new object[] { Null.Value };
            yield return new object[] { new Binary(0x00) };
            yield return new object[] { new Bencodex.Types.Boolean(false) };
            yield return new object[] { new Integer(0) };
            yield return new object[] { new Text("test") };
            yield return new object[] { List.Empty };
            yield return new object[]
            {
                new List((Text)"test", (Integer)1),
            };
            yield return new object[]
            {
                new List(Null.Value, (Integer)1, (Integer)0),
            };
            yield return new object[]
            {
                new List((Text)"test", (Integer)(-1), (Integer)0),
            };
            yield return new object[]
            {
                new List((Text)"test", (Integer)1, (Integer)0, (Integer)0),
            };
            yield return new object[] { Dictionary.Empty };
        }

        [Fact]
        public void Serialize()
        {
            // v1
            var expected = new List(
                (Text)ITestStateV1.Moniker,
                (Integer)ITestStateV1.Version,
                (Integer)0);
            var serialized = VersionedStateFormatter.Serialize(
                (Text)ITestStateV1.Moniker,
                (Integer)ITestStateV1.Version,
                new TestStateV1(0).Serialize());
            Assert.Equal(expected, serialized);
            serialized = VersionedStateFormatter.Serialize(
                ITestStateV1.Moniker,
                ITestStateV1.Version,
                new TestStateV1(0).Serialize());
            Assert.Equal(expected, serialized);
            serialized = VersionedStateFormatter.Serialize(
                ITestStateV1.Moniker,
                ITestStateV1.Version,
                new TestStateV1(0));
            Assert.Equal(expected, serialized);
            // v2
            expected = new List(
                (Text)ITestStateV2.Moniker,
                (Integer)ITestStateV2.Version,
                new List((Integer)0, (Text)"v2"));
            serialized = VersionedStateFormatter.Serialize(
                (Text)ITestStateV2.Moniker,
                (Integer)ITestStateV2.Version,
                new TestStateV2(0, "v2").Serialize());
            Assert.Equal(expected, serialized);
            serialized = VersionedStateFormatter.Serialize(
                ITestStateV2.Moniker,
                ITestStateV2.Version,
                new TestStateV2(0, "v2").Serialize());
            Assert.Equal(expected, serialized);
            serialized = VersionedStateFormatter.Serialize(
                ITestStateV2.Moniker,
                ITestStateV2.Version,
                new TestStateV2(0, "v2"));
            Assert.Equal(expected, serialized);
            // v3
            expected = new List(
                (Text)ITestStateV3.Moniker,
                (Integer)ITestStateV3.Version,
                new List((Text)"v3", (Integer)10));
            serialized = VersionedStateFormatter.Serialize(
                (Text)ITestStateV3.Moniker,
                (Integer)ITestStateV3.Version,
                new TestStateV3("v3", 10).Serialize());
            Assert.Equal(expected, serialized);
            serialized = VersionedStateFormatter.Serialize(
                ITestStateV3.Moniker,
                ITestStateV3.Version,
                new TestStateV3("v3", 10).Serialize());
            Assert.Equal(expected, serialized);
            serialized = VersionedStateFormatter.Serialize(
                ITestStateV3.Moniker,
                ITestStateV3.Version,
                new TestStateV3("v3", 10));
            Assert.Equal(expected, serialized);
        }

        [Fact]
        public void Deconstruct()
        {
            // v1
            var (moniker, version, data) =
                VersionedStateFormatter.Deconstruct(
                    new List(
                        (Text)ITestStateV1.Moniker,
                        (Integer)ITestStateV1.Version,
                        (Integer)0
                    )
                );
            Assert.Equal(ITestStateV1.Moniker, moniker);
            Assert.Equal(ITestStateV1.Version, (uint)version);
            Assert.Equal(0, (int)(Integer)data);
        }

        [Fact]
        public void Deconstruct_Throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                VersionedStateFormatter.Deconstruct(null));
        }

        [Theory]
        [MemberData(nameof(GetMemberData_For_TestStateV1_InvalidSerializedValue))]
        public void Deconstruct_Throws_ArgumentException(IValue serialized)
        {
            Assert.Throws<ArgumentException>(() =>
                VersionedStateFormatter.Deconstruct(serialized));
        }

        [Fact]
        public void TryDeconstruct()
        {
            // v1
            var v1Val = new List(
                (Text)ITestStateV1.Moniker,
                (Integer)ITestStateV1.Version,
                (Integer)0);
            Assert.True(VersionedStateFormatter.TryDeconstruct(
                v1Val,
                out var result));
            Assert.Equal(ITestStateV1.Moniker, result.moniker);
            Assert.Equal(ITestStateV1.Version, (uint)result.version);
            Assert.Equal(0, (int)(Integer)result.data);
        }

        [Fact]
        public void DeconstructT()
        {
            // v1
            var v1 = new TestStateV1(0);
            var (moniker, version, data) =
                VersionedStateFormatter.Deconstruct(v1);
            Assert.Equal(ITestStateV1.Moniker, moniker);
            Assert.Equal(ITestStateV1.Version, (uint)version);
            Assert.Equal(0, (int)(Integer)data);
            Assert.Equal(v1.Serialize(), data);
            (moniker, version, data) =
                VersionedStateFormatter.Deconstruct(v1);
            Assert.Equal(ITestStateV1.Moniker, moniker);
            Assert.Equal(ITestStateV1.Version, (uint)version);
            Assert.Equal(0, (int)(Integer)data);

            Assert.Throws<ArgumentNullException>(() =>
                VersionedStateFormatter.Deconstruct(null));
            Assert.Throws<ArgumentException>(() =>
                VersionedStateFormatter.Deconstruct(new TestState(0)));
            Assert.Throws<ArgumentException>(() =>
                VersionedStateFormatter.Deconstruct(
                    new TestState_InvalidVersionedStateImplType(0)));
        }

        [Fact]
        public void TryDeconstructT()
        {
            // v1
            Assert.True(VersionedStateFormatter.TryDeconstruct(
                new TestStateV1(0),
                out var result));
            Assert.Equal(ITestStateV1.Moniker, result.moniker);
            Assert.Equal(ITestStateV1.Version, (uint)result.version);
            Assert.Equal(0, (int)(Integer)result.data);
        }

        [Fact]
        public void ValidateFormat_Returns_True()
        {
            Assert.True(VersionedStateFormatter.ValidateFormat(
                new List(
                    (Text)ITestStateV1.Moniker,
                    (Integer)ITestStateV1.Version,
                    (Integer)0
                )
            ));
        }

        [Theory]
        [MemberData(nameof(GetMemberData_For_TestStateV1_InvalidSerializedValue))]
        public void ValidateFormat_Returns_False(IValue serialized)
        {
            Assert.False(VersionedStateFormatter.ValidateFormat(serialized));
        }
    }
}
