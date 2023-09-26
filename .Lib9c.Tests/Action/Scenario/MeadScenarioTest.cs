namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Module;
    using Xunit;

    public class MeadScenarioTest
    {
        [Fact]
        public void Contract()
        {
            Currency mead = Currencies.Mead;
            var patron = new PrivateKey().ToAddress();
            IActionContext context = new ActionContext();
            IWorld states = new MockWorld();
            states = LegacyModule.MintAsset(states, context, patron, 10 * mead);

            var agentAddress = new PrivateKey().ToAddress();
            var requestPledge = new RequestPledge
            {
                AgentAddress = agentAddress,
                RefillMead = RequestPledge.DefaultRefillMead,
            };
            var states2 = Execute(context, states, requestPledge, patron);
            Assert.Equal(8 * mead, LegacyModule.GetBalance(states2, patron, mead));
            Assert.Equal(1 * mead, LegacyModule.GetBalance(states2, agentAddress, mead));

            var approvePledge = new ApprovePledge
            {
                PatronAddress = patron,
            };
            var states3 = Execute(context, states2, approvePledge, agentAddress);
            Assert.Equal(4 * mead, LegacyModule.GetBalance(states3, patron, mead));
            Assert.Equal(4 * mead, LegacyModule.GetBalance(states3, agentAddress, mead));

            // release and return agent mead
            var endPledge = new EndPledge
            {
                AgentAddress = agentAddress,
            };
            var states4 = Execute(context, states3, endPledge, patron);
            Assert.Equal(7 * mead, LegacyModule.GetBalance(states4, patron, mead));
            Assert.Equal(0 * mead, LegacyModule.GetBalance(states4, agentAddress, mead));

            // re-contract with Bencodex.Null
            var states5 = Execute(context, states4, requestPledge, patron);
            Assert.Equal(5 * mead, LegacyModule.GetBalance(states5, patron, mead));
            Assert.Equal(1 * mead, LegacyModule.GetBalance(states5, agentAddress, mead));

            var states6 = Execute(context, states5, approvePledge, agentAddress);
            Assert.Equal(1 * mead, LegacyModule.GetBalance(states6, patron, mead));
            Assert.Equal(4 * mead, LegacyModule.GetBalance(states6, agentAddress, mead));
        }

        [Fact]
        public void UseGas()
        {
            Type baseType = typeof(Nekoyume.Action.ActionBase);

            bool IsTarget(Type type)
            {
                return baseType.IsAssignableFrom(type) &&
                    type != typeof(InitializeStates) &&
                    type.GetCustomAttribute<ActionTypeAttribute>() is { } &&
                    (
                        !(type.GetCustomAttribute<ActionObsoleteAttribute>()?.ObsoleteIndex is { } obsoleteIndex) ||
                        obsoleteIndex > ActionObsoleteConfig.V200030ObsoleteIndex
                    );
            }

            var assembly = baseType.Assembly;
            var typeIds = assembly.GetTypes()
                .Where(IsTarget);
            long expectedTransferActionGasLimit = 4L;
            long expectedActionGasLimit = 1L;
            foreach (var typeId in typeIds)
            {
                var action = (IAction)Activator.CreateInstance(typeId)!;
                var actionContext = new ActionContext
                {
                    PreviousState = new MockWorld(),
                };
                try
                {
                    action.Execute(actionContext);
                }
                catch (Exception)
                {
                    // ignored
                }

                long expectedGasLimit = action is ITransferAsset || action is ITransferAssets
                    ? expectedTransferActionGasLimit
                    : expectedActionGasLimit;
                long gasUsed = actionContext.GasUsed();
                Assert.True(expectedGasLimit == gasUsed, $"{action} invalid used gas. {gasUsed}");
            }
        }

        private IWorld Execute(IActionContext context, IWorld state, IAction action, Address signer)
        {
            Assert.True(LegacyModule.GetBalance(state, signer, Currencies.Mead) > 0 * Currencies.Mead);
            var nextState = LegacyModule.BurnAsset(state, context, signer, 1 * Currencies.Mead);
            var executedState = action.Execute(new ActionContext
            {
                Signer = signer,
                PreviousState = nextState,
            });
            return RewardGold.TransferMead(context, executedState);
        }
    }
}
