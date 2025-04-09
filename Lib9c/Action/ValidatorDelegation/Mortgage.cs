using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Nekoyume.Model.Guild;
using Nekoyume.Model.State;
using Nekoyume.Module.Guild;
using Nekoyume.Module;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;
using Lib9c;

namespace Nekoyume.Action.ValidatorDelegation
{
    /// <summary>
    /// An action for mortgage gas fee for a transaction.
    /// Should be executed at the beginning of the tx.
    /// </summary>
    public sealed class Mortgage : ActionBase
    {
        /// <summary>
        /// Creates a new instance of <see cref="Mortgage"/>.
        /// </summary>
        public Mortgage()
        {
        }

        /// <inheritdoc cref="IAction.PlainValue"/>
        public override IValue PlainValue => new Bencodex.Types.Boolean(true);

        /// <inheritdoc cref="IAction.LoadPlainValue(IValue)"/>
        public override void LoadPlainValue(IValue plainValue)
        {
            // Method intentionally left empty.
        }

        /// <inheritdoc cref="IAction.Execute(IActionContext)"/>
        public override IWorld Execute(IActionContext context)
        {
            var state = context.PreviousState;
            if (context.MaxGasPrice is not { Sign: > 0 } realGasPrice)
            {
                return state;
            }

            var gasRequired = realGasPrice * GasTracer.GasAvailable;
            var guildRepo = new GuildRepository(state, context);
            var joinedGuildAddress = guildRepo.GetJoinedGuild(new AgentAddress(context.Signer));
            if (joinedGuildAddress is GuildAddress guildAddress)
            {
                var guildGasBalance = state.GetBalance(guildAddress, realGasPrice.Currency);
                if (guildGasBalance >= gasRequired)
                {
                    if (state.GetLegacyState(context.Signer.GetPledgeAddress()) is List contract
                        && contract[1].ToBoolean()
                        && contract[0].ToAddress() == MeadConfig.PatronAddress
                        && contract[2].ToInteger() * Currencies.Mead >= gasRequired)
                    {
                        return PayMaster.Mortgage(state, context, context.Signer, guildAddress, gasRequired);
                    }
                }
            }

            var gasOwned = state.GetBalance(context.Signer, realGasPrice.Currency);
            var gasToMortgage = gasOwned < gasRequired ? gasOwned : gasRequired;
            if (gasOwned < gasRequired)
            {
                GasTracer.CancelTrace();
            }

            if (gasToMortgage.Sign > 0)
            {
                return state.TransferAsset(
                    context, context.Signer, Addresses.MortgagePool, gasToMortgage);
            }

            return state;
        }
    }
}
