namespace Lib9c.Tests.Action.Factory
{
#nullable enable

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Bencodex.Types;
    using Lib9c.Abstractions;
    using Lib9c.Action;
    using Lib9c.Action.Factory;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Xunit;

    public class ClaimStakeRewardFactoryTest
    {
        public static IEnumerable<object[]> GetAllClaimStakeRewardV1()
        {
            var arr = Assembly.GetAssembly(typeof(ClaimRaidReward))?.GetTypes()
                .Where(
                    type =>
                        type.IsClass &&
                        typeof(IClaimStakeRewardV1).IsAssignableFrom(type))
                .Select(
                    type =>
                        type.GetCustomAttribute<ActionTypeAttribute>()?.TypeIdentifier)
                .OfType<IValue>()
                .ToArray() ?? Array.Empty<IValue>();

            foreach (var value in arr)
            {
                var str = (string)(Text)value;
                var verStr = str.Replace("claim_stake_reward", string.Empty);
                var ver = string.IsNullOrEmpty(verStr)
                    ? 1
                    : int.Parse(verStr);
                yield return new object[] { str, ver, };
            }
        }

        [Theory]
        [InlineData(ClaimStakeReward8.ObsoleteBlockIndex + 1, typeof(ClaimStakeReward))]
        [InlineData(long.MaxValue, typeof(ClaimStakeReward))]
        public void Create_ByBlockIndex_Success(
            long blockIndex,
            Type type)
        {
            var addr = new PrivateKey().Address;
            var action = ClaimStakeRewardFactory.CreateByBlockIndex(blockIndex, addr);
            Assert.Equal(type, action.GetType());
        }

        [Theory]
        [MemberData(nameof(GetAllClaimStakeRewardV1))]
        public void Create_ByVersion_Success(string expectActionType, int version)
        {
            var addr = new PrivateKey().Address;
            var action = ClaimStakeRewardFactory.CreateByVersion(version, addr);
            var actualActionType = action
                .GetType()
                .GetCustomAttribute<ActionTypeAttribute>()?.TypeIdentifier;
            Assert.Equal(new Text(expectActionType), actualActionType);
        }
    }
}
