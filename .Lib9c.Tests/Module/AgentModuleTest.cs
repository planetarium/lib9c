namespace Lib9c.Tests.Module
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class AgentModuleTest
    {
        private readonly Address _address;
        private readonly Dictionary<int, Address> _avatarAddresses;
        private readonly HashSet<int> _unlockedOptions;
        private readonly int _monsterCollectionRound;

        public AgentModuleTest()
        {
            _address = new PrivateKey().ToAddress();
            _avatarAddresses = new Dictionary<int, Address>
            {
                [0] = new PrivateKey().ToAddress(),
                [1] = new PrivateKey().ToAddress(),
                [2] = new PrivateKey().ToAddress(),
            };
            _unlockedOptions = new HashSet<int> { 0, 1, 2, 3, 4, 5 };
            _monsterCollectionRound = 3;
        }

        [Fact]
        public void GetAgentStateV0()
        {
            var dict = new Dictionary<IKey, IValue>
            {
                [(Text)LegacyAddressKey] = _address.Serialize(),
                [(Text)"avatarAddresses"] = new Dictionary(
                    _avatarAddresses.Select(kv =>
                        new KeyValuePair<IKey, IValue>(
                            new Binary(BitConverter.GetBytes(kv.Key)),
                            kv.Value.Serialize()
                        )
                    )
                ),
                [(Text)"unlockedOptions"] = _unlockedOptions.Select(i => i.Serialize()).Serialize(),
                [(Text)MonsterCollectionRoundKey] = _monsterCollectionRound.Serialize(),
            };

            IWorld world = new MockWorld();
            IAccount account = new MockAccount();
            account = account.SetState(_address, new Dictionary(dict));
            world = world.SetAccount(ReservedAddresses.LegacyAccount, account);
            var agentStateV0 = AgentModule.GetAgentState(world, _address);
            Assert.NotNull(agentStateV0);
            Assert.Equal(0, agentStateV0.Version);
            Assert.Equal(_address, agentStateV0.address);
            Assert.Equal(_avatarAddresses, agentStateV0.avatarAddresses);
            Assert.Equal(_unlockedOptions, agentStateV0.unlockedOptions);
            Assert.Equal(_monsterCollectionRound, agentStateV0.MonsterCollectionRound);
        }

        [Fact]
        public void GetAgentStateV1()
        {
            int version = 1;
            var list = new List<IValue>
            {
                new List(_address.Serialize()),
                (Integer)version,
                new Dictionary(
                    _avatarAddresses.Select(kv =>
                        new KeyValuePair<IKey, IValue>(
                            new Binary(BitConverter.GetBytes(kv.Key)),
                            kv.Value.Serialize()
                        )
                    )
                ),
                _unlockedOptions.Select(i => i.Serialize()).Serialize(),
                _monsterCollectionRound.Serialize(),
            };

            IWorld world = new MockWorld();
            IAccount account = new MockAccount();
            account = account.SetState(_address, new List(list));
            world = world.SetAccount(Addresses.Agent, account);
            var agentStateV1 = AgentModule.GetAgentState(world, _address);
            Assert.NotNull(agentStateV1);
            Assert.Equal(version, agentStateV1.Version);
            Assert.Equal(_address, agentStateV1.address);
            Assert.Equal(_avatarAddresses, agentStateV1.avatarAddresses);
            Assert.Equal(_unlockedOptions, agentStateV1.unlockedOptions);
            Assert.Equal(_monsterCollectionRound, agentStateV1.MonsterCollectionRound);
        }
    }
}
