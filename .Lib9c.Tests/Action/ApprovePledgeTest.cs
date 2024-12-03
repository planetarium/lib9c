namespace Lib9c.Tests.Action
{
    using System;
    using Bencodex.Types;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Mocks;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Guild;
    using Nekoyume.Action.ValidatorDelegation;
    using Nekoyume.Model.Guild;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.Module.Guild;
    using Nekoyume.TypedAddress;
    using Xunit;

    public class ApprovePledgeTest
    {
        [Theory]
        [InlineData(RequestPledge.DefaultRefillMead)]
        [InlineData(100)]
        public void Execute(int mead)
        {
            var address = new PrivateKey().Address;
            var patron = new PrivateKey().Address;
            var contractAddress = address.Derive(nameof(RequestPledge));
            var states = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(
                    contractAddress,
                    List.Empty.Add(patron.Serialize()).Add(false.Serialize()).Add(mead.Serialize())
                );

            var action = new ApprovePledge
            {
                PatronAddress = patron,
            };
            var nextState = action.Execute(
                new ActionContext
                {
                    Signer = address,
                    PreviousState = states,
                });

            var contract = Assert.IsType<List>(nextState.GetLegacyState(contractAddress));
            Assert.Equal(patron, contract[0].ToAddress());
            Assert.True(contract[1].ToBoolean());
            Assert.Equal(mead, contract[2].ToInteger());
            Assert.Null(new GuildRepository(nextState, new ActionContext())
                .GetJoinedGuild(new AgentAddress(address)));
        }

        [Theory]
        [InlineData(RequestPledge.DefaultRefillMead)]
        [InlineData(100)]
        public void Execute_JoinGuild(int mead)
        {
            var address = new PrivateKey().Address;
            var validatorKey = new PrivateKey();
            var patron = MeadConfig.PatronAddress;
            var contractAddress = address.Derive(nameof(RequestPledge));
            var guildAddress = AddressUtil.CreateGuildAddress();
            var states = new World(MockUtil.MockModernWorldState)
                .SetLegacyState(
                    Addresses.GoldCurrency,
                    new GoldCurrencyState(Currency.Legacy("NCG", 2, null)).Serialize())
                .SetLegacyState(
                    contractAddress,
                    List.Empty.Add(patron.Serialize()).Add(false.Serialize()).Add(mead.Serialize())
                );

            states = DelegationUtil.EnsureValidatorPromotionReady(
                states, validatorKey.PublicKey, 0L);

            states = new GuildRepository(
                states,
                new ActionContext
                {
                    Signer = GuildConfig.PlanetariumGuildOwner,
                })
                .MakeGuild(guildAddress, validatorKey.Address).World;

            var action = new ApprovePledge
            {
                PatronAddress = patron,
            };
            var nextState = action.Execute(
                new ActionContext
                {
                    Signer = address,
                    PreviousState = states,
                });

            var contract = Assert.IsType<List>(nextState.GetLegacyState(contractAddress));
            Assert.Equal(patron, contract[0].ToAddress());
            Assert.True(contract[1].ToBoolean());
            Assert.Equal(mead, contract[2].ToInteger());
            var joinedGuildAddress = new GuildRepository(nextState, new ActionContext())
                .GetJoinedGuild(new AgentAddress(address));
            Assert.NotNull(joinedGuildAddress);
            Assert.Equal(guildAddress, joinedGuildAddress);
        }

        [Theory]
        [InlineData(false, false, typeof(FailedLoadStateException))]
        [InlineData(true, false, typeof(InvalidAddressException))]
        [InlineData(false, true, typeof(AlreadyContractedException))]
        public void Execute_Throw_Exception(bool invalidPatron, bool alreadyContract, Type exc)
        {
            var address = new PrivateKey().Address;
            var patron = new PrivateKey().Address;
            var contractAddress = address.Derive(nameof(RequestPledge));
            IValue contract = Null.Value;
            if (invalidPatron)
            {
                contract = List.Empty.Add(new PrivateKey().Address.Serialize());
            }

            if (alreadyContract)
            {
                contract = List.Empty.Add(patron.Serialize()).Add(true.Serialize());
            }

            var states = new World(MockUtil.MockModernWorldState).SetLegacyState(contractAddress, contract);

            var action = new ApprovePledge
            {
                PatronAddress = patron,
            };
            Assert.Throws(
                exc,
                () => action.Execute(
                    new ActionContext
                    {
                        Signer = address,
                        PreviousState = states,
                    }));
        }
    }
}
