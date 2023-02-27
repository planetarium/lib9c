namespace Lib9c.Tests.VersionedStates
{
    using System;
    using System.Linq;
    using Bencodex.Types;
    using Nekoyume.VersionedStates;
    using Snapshooter;
    using Snapshooter.Xunit;
    using Xunit;

    public class VersionedStateSnapshotTest
    {
        [Fact]
        public void SnapshotTest()
        {
            var iVersionedStateType = typeof(IVersionedState);
            var tuples = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(type =>
                    !type.IsAbstract &&
                    !type.IsInterface &&
                    type.IsAssignableTo(iVersionedStateType))
                .Select(type =>
                {
                    var state = (IVersionedState)Activator.CreateInstance(type);
                    return (
                        type: type,
                        value: state?.Serialize() ?? Null.Value);
                })
                .ToArray();
            foreach (var (type, value) in tuples)
            {
                Snapshot.Match(value, SnapshotNameExtension.Create(type.FullName));
            }
        }
    }
}
