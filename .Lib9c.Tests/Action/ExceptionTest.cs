namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Security.Cryptography;
    using Bencodex.Types;
    using Lib9c.Formatters;
    using Libplanet.Action;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Libplanet.Types.Blocks;
    using Libplanet.Types.Evidence;
    using Libplanet.Types.Tx;
    using MessagePack;
    using MessagePack.Resolvers;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class ExceptionTest
    {
        public ExceptionTest()
        {
            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                NineChroniclesResolver.Instance,
                StandardResolver.Instance
            );
            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            MessagePackSerializer.DefaultOptions = options;
        }

        public static IEnumerable<object[]> GetLibplanetExceptions()
        {
            var t = typeof(Exception);
            var exceptions = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(e => e.GetTypes())
                .Where(e =>
                    e.Namespace is not null &&
                    e.Namespace.StartsWith("Libplanet") &&
                    !e.IsAbstract &&
                    e.IsClass &&
                    e.IsAssignableTo(t))
                .ToArray();
            foreach (var e in exceptions)
            {
                if (e == typeof(InvalidBlockProtocolVersionException) ||
                    e == typeof(InvalidBlockStateRootHashException) ||
                    e == typeof(DuplicateVoteException))
                {
                    // FIXME:
                    // MessagePack.MessagePackSerializationException: Failed to serialize System.Exception value.
                    continue;
                }

                yield return new object[] { e, };
            }
        }

        public static IEnumerable<object[]> GetLib9cExceptions()
        {
            var t = typeof(Exception);
            var exceptions = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(e => e.GetTypes())
                .Where(e =>
                    e.Namespace is not null &&
                    e.Namespace.StartsWith("Nekoyume") &&
                    !e.IsAbstract &&
                    e.IsClass &&
                    e.IsAssignableTo(t))
                .ToArray();
            foreach (var e in exceptions)
            {
                yield return new object[] { e, };
            }
        }

        [Theory]
        [MemberData(nameof(GetLibplanetExceptions))]
        [MemberData(nameof(GetLib9cExceptions))]
        public void Exception_Serializable(Type excType)
        {
            var constructorTuples = excType.GetConstructors()
                .Select(e => (constructorInfo: e, parameters: e.GetParameters()))
                .OrderBy(tuple => tuple.parameters.Length);
            foreach (var (constructorInfo, parameters) in constructorTuples)
            {
                var parametersLength = parameters.Length;
                if (parametersLength == 0)
                {
                    AssertException((Exception)constructorInfo.Invoke(Array.Empty<object>()));
                    return;
                }

                var found = true;
                var parameterValues = new List<object>();
                for (var i = 0; i < parametersLength; i++)
                {
                    if (TryGetDefaultValue(parameters[i].ParameterType, out var value))
                    {
                        parameterValues.Add(value);
                    }
                    else
                    {
                        found = false;
                        break;
                    }
                }

                if (!found)
                {
                    continue;
                }

                AssertException((Exception)constructorInfo.Invoke(parameterValues.ToArray()));
                return;
            }

            throw new InvalidOperationException($"No suitable constructor found for {excType.FullName}.");

            bool TryGetDefaultValue(Type type, out object value)
            {
                if (Nullable.GetUnderlyingType(type) != null)
                {
                    value = null;
                    return true;
                }

                if (type.IsClass)
                {
                    value = null;
                    return true;
                }

                if (type == typeof(bool))
                {
                    value = default(bool);
                    return true;
                }

                if (type == typeof(int))
                {
                    value = default(int);
                    return true;
                }

                if (type == typeof(long))
                {
                    value = default(long);
                    return true;
                }

                if (type == typeof(string))
                {
                    value = "for testing";
                    return true;
                }

                if (type.IsAssignableTo(typeof(Exception)))
                {
                    value = null;
                    return true;
                }

                if (type == typeof(HashDigest<SHA256>))
                {
                    value = HashDigest<SHA256>.FromString("baa2081d3b485ef2906c95a3965531ec750a74cfaefe91d0c3061865608b426c");
                    return true;
                }

                if (type == typeof(ImmutableArray<byte>))
                {
                    value = ImmutableArray<byte>.Empty;
                    return true;
                }

                if (type == typeof(IImmutableSet<Type>))
                {
                    value = ImmutableHashSet<Type>.Empty;
                    return true;
                }

                if (type == typeof(IAction))
                {
                    value = new DailyReward
                    {
                        avatarAddress = Addresses.Agent,
                    };
                    return true;
                }

                if (type == typeof(IValue))
                {
                    value = Bencodex.Types.Null.Value;
                    return true;
                }

                if (type == typeof(Address))
                {
                    value = Nekoyume.Addresses.Admin;
                    return true;
                }

                if (type == typeof(Currency))
                {
                    value = Currencies.Crystal;
                    return true;
                }

                if (type == typeof(FungibleAssetValue))
                {
                    value = FungibleAssetValue.Parse(Currencies.Crystal, "1");
                    return true;
                }

                if (type == typeof(BlockHash))
                {
                    value = BlockHash.FromString("4582250d0da33b06779a8475d283d5dd210c683b9b999d74d03fac4f58fa6bce");
                    return true;
                }

                if (type == typeof(TxId))
                {
                    value = TxId.FromHex("300826da62b595d8cd663dadf04995a7411534d1cdc17dac75ce88754472f774");
                    return true;
                }

                value = null;
                return false;
            }
        }

        [Fact(Skip = "FIXME: Cannot serialize AdminState with MessagePackSerializer")]
        public void AdminPermissionExceptionSerializable()
        {
            var policy = new AdminState(default, 100);
            var address = new Address("399bddF9F7B6d902ea27037B907B2486C9910730");
            var exc = new PermissionDeniedException(policy, address);
            AssertException<PermissionDeniedException>(exc);
            var formatter = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                formatter.Serialize(ms, exc);

                ms.Seek(0, SeekOrigin.Begin);
                var deserialized = (PermissionDeniedException)formatter.Deserialize(ms);
                AssertAdminState(exc.Policy, deserialized.Policy);
                Assert.Equal(exc.Signer, deserialized.Signer);
            }

            var exc2 = new PolicyExpiredException(policy, 101);
            AssertException<PolicyExpiredException>(exc2);
            var formatter2 = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                formatter2.Serialize(ms, exc2);

                ms.Seek(0, SeekOrigin.Begin);
                var deserialized = (PolicyExpiredException)formatter2.Deserialize(ms);
                AssertAdminState(exc2.Policy, deserialized.Policy);
                Assert.Equal(exc2.BlockIndex, deserialized.BlockIndex);
            }
        }

        private static void AssertException<T>(Exception exc)
            where T : Exception
        {
            AssertException(exc);
        }

        private static void AssertException(Exception exc)
        {
            var b = MessagePackSerializer.Serialize(exc);
            var des = MessagePackSerializer.Deserialize<Exception>(b);
            Assert.Equal(exc.Message, des.Message);
        }

        private static void AssertAdminState(AdminState adminState, AdminState adminState2)
        {
            Assert.Equal(adminState.AdminAddress, adminState2.AdminAddress);
            Assert.Equal(adminState.address, adminState2.address);
            Assert.Equal(adminState.ValidUntil, adminState2.ValidUntil);
        }
    }
}
