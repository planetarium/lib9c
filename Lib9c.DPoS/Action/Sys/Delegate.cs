using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.DPoS.Control;
using Lib9c.DPoS.Misc;
using Lib9c.DPoS.Util;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Lib9c.DPoS.Action.Sys
{
    /// <summary>
    /// A system action for DPoS that <see cref="Delegate"/> specified <see cref="Amount"/>
    /// of tokens to a given <see cref="Validator"/>.
    /// </summary>
    public sealed class Delegate : IAction
    {
        /// <summary>
        /// Creates a new instance of <see cref="Delegate"/> action.
        /// </summary>
        /// <param name="validator">The <see cref="Address"/> of the validator
        /// to delegate tokens.</param>
        /// <param name="amount">The amount of the asset to be delegated.</param>
        public Delegate(Address validator, FungibleAssetValue amount)
        {
            Validator = validator;
            Amount = amount;
        }

        internal Delegate()
        {
            // Used only for deserialization.  See also class Libplanet.Action.Sys.Registry.
        }

        /// <summary>
        /// The <see cref="Address"/> of the validator to <see cref="Delegate"/>.
        /// </summary>
        public Address Validator { get; set; }

        public FungibleAssetValue Amount { get; set; }

        /// <inheritdoc cref="IAction.PlainValue"/>
        public IValue PlainValue => Bencodex.Types.Dictionary.Empty
            .Add("validator", Validator.Serialize())
            .Add("amount", Amount.Serialize());

        /// <inheritdoc cref="IAction.LoadPlainValue(IValue)"/>
        public void LoadPlainValue(IValue plainValue)
        {
            var dict = (Bencodex.Types.Dictionary)plainValue;
            Validator = dict["validator"].ToAddress();
            Amount = dict["amount"].ToFungibleAssetValue();
        }

        /// <inheritdoc cref="IAction.Execute(IActionContext)"/>
        public IWorld Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousState;

            // if (ctx.Rehearsal)
            // Rehearsal mode is not implemented
            var nativeTokens = ImmutableHashSet.Create(
                Asset.GovernanceToken, Asset.ConsensusToken, Asset.Share);
            states = DelegateCtrl.Execute(
                states,
                ctx,
                ctx.Signer,
                Validator,
                Amount,
                nativeTokens);

            return states;
        }
    }
}
