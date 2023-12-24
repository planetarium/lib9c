namespace Lib9c.Tests.Action
{
    using System;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
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
            IWorld states = new World(new MockWorldState())
                .SetLegacyState(
                    contractAddress,
                    List.Empty.Add(patron.Serialize()).Add(false.Serialize()).Add(mead.Serialize())
                );

            var action = new ApprovePledge
            {
                PatronAddress = patron,
            };
            var nextState = action.Execute(new ActionContext
            {
                Signer = address,
                PreviousState = states,
            });

            var contract = Assert.IsType<List>(nextState.GetLegacyState(contractAddress));
            Assert.Equal(patron, contract[0].ToAddress());
            Assert.True(contract[1].ToBoolean());
            Assert.Equal(mead, contract[2].ToInteger());
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

            IWorld states = new World(new MockWorldState()).SetLegacyState(contractAddress, contract);

            var action = new ApprovePledge
            {
                PatronAddress = patron,
            };
            Assert.Throws(exc, () => action.Execute(new ActionContext
            {
                Signer = address,
                PreviousState = states,
            }));
        }
    }
}
