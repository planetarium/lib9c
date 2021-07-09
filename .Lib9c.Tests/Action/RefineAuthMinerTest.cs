namespace Lib9c.Tests.Action
{
    using System.Collections.Immutable;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class RefineAuthMinerTest
    {
        [Fact]
        public void Execute()
        {
            var admin = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            var miners = GetNewMiners(4);
            var state = new State(
                ImmutableDictionary<Address, IValue>.Empty
                    .Add(AdminState.Address, new AdminState(admin, 100).Serialize())
                    .Add(AuthorizedMinersState.Address, new AuthorizedMinersState(
                        miners: miners,
                        interval: 50,
                        validUntil: 1000).Serialize())
            );

            var updatedMiners = GetNewMiners(10);
            var action = new RefineAuthMiner(
                2,
                5000,
                updatedMiners.ToList()
            );

            IAccountStateDelta nextState = action.Execute(new ActionContext()
            {
                BlockIndex = 1,
                Miner = default,
                PreviousStates = state,
                Signer = admin,
            });

            var nextAuthMinersStates = new AuthorizedMinersState(
                (Dictionary)nextState.GetState(AuthorizedMinersState.Address)!
            );

            Assert.Equal(2, nextAuthMinersStates.Interval);
            Assert.Equal(5000, nextAuthMinersStates.ValidUntil);
            Assert.Equal(updatedMiners.ToImmutableHashSet(), nextAuthMinersStates.Miners);
        }

        [Fact]
        public void Rehearsal()
        {
            var admin = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            var miners = GetNewMiners(4);
            var state = new State(
                ImmutableDictionary<Address, IValue>.Empty
                    .Add(AdminState.Address, new AdminState(admin, 100).Serialize())
                    .Add(AuthorizedMinersState.Address, new AuthorizedMinersState(
                        miners: miners,
                        interval: 50,
                        validUntil: 1000).Serialize())
            );

            var updatedMiners = GetNewMiners(10);
            var action = new RefineAuthMiner(
                2,
                5000,
                updatedMiners.ToList()
            );

            IAccountStateDelta nextState = action.Execute(new ActionContext()
            {
                BlockIndex = 1,
                Miner = default,
                PreviousStates = state,
                Signer = admin,
                Rehearsal = true,
            });

            Assert.Contains(AuthorizedMinersState.Address, nextState.UpdatedAddresses);
            Assert.Equal(ActionBase.MarkChanged, nextState.GetState(AuthorizedMinersState.Address));
        }

        [Fact]
        public void CheckPermission()
        {
            var admin = new Address("8d9f76aF8Dc5A812aCeA15d8bf56E2F790F47fd7");
            var miners = GetNewMiners(4);
            var state = new State(
                ImmutableDictionary<Address, IValue>.Empty
                    .Add(AdminState.Address, new AdminState(admin, 100).Serialize())
                    .Add(AuthorizedMinersState.Address, new AuthorizedMinersState(
                        miners: miners,
                        interval: 50,
                        validUntil: 1000).Serialize())
            );

            var updatedMiners = GetNewMiners(10);
            var action = new RefineAuthMiner(
                2,
                5000,
                updatedMiners.ToList()
            );

            Assert.Throws<PermissionDeniedException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    BlockIndex = 1,
                    Miner = default,
                    PreviousStates = state,
                    Signer = new PrivateKey().ToAddress(),
                });
            });

            Assert.Throws<PolicyExpiredException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    BlockIndex = 101,
                    Miner = default,
                    PreviousStates = state,
                    Signer = admin,
                });
            });
        }

        private static Address[] GetNewMiners(int count)
        {
            return Enumerable.Range(0, count).Select(i => new PrivateKey().ToAddress()).ToArray();
        }
    }
}