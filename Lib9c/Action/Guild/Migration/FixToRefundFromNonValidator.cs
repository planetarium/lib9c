using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.Stake;
using Nekoyume.Model.State;

namespace Nekoyume.Action.Guild.Migration
{
    // TODO: [GuildMigration] Remove this class when the migration is done.
    /// <summary>
    /// An action to fix refund from non-validator.
    /// </summary>
    [ActionType(TypeIdentifier)]
    public class FixToRefundFromNonValidator : ActionBase
    {
        public const string TypeIdentifier = "fix_to_refund_from_non_validator";

        private const string TargetsKey = "t";

        private const string AmountsKey = "a";

        public List<Address> Targets { get; private set; }

        public List<int> Amounts { get; private set; }

        public FixToRefundFromNonValidator()
        {
        }

        public FixToRefundFromNonValidator(
            IEnumerable<Address> targets,
            IEnumerable<int> amounts)
        {
            Targets = targets.ToList();
            Amounts = amounts.ToList();

            if (Targets.Count != Amounts.Count)
            {
                throw new ArgumentException("The number of targets and amounts must be the same.");
            }
        }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Dictionary.Empty
                .Add(TargetsKey, new List(Targets.Select(t => t.Bencoded)))
                .Add(AmountsKey, new List(Amounts)));

        public override void LoadPlainValue(IValue plainValue)
        {
            if (plainValue is not Dictionary root ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not Dictionary values ||
                !values.TryGetValue((Text)TargetsKey, out var rawTarget) ||
                rawTarget is not List targets ||
                !values.TryGetValue((Text)AmountsKey, out var rawAmounts) ||
                rawAmounts is not List amounts)
            {
                throw new InvalidCastException();
            }

            Targets = targets.Select(t => new Address(t)).ToList();
            Amounts = amounts.Select(a => (int)(Integer)a).ToList();

            if (Targets.Count != Amounts.Count)
            {
                throw new ArgumentException("The number of targets and amounts must be the same.");
            }
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            var world = context.PreviousState;

            if (!TryGetAdminState(context, out AdminState adminState))
            {
                throw new InvalidOperationException("Couldn't find admin state");
            }

            if (context.Signer != adminState.AdminAddress)
            {
                throw new PermissionDeniedException(adminState, context.Signer);
            }

            foreach (var ta in Targets.Zip(Amounts, (f, s) => (f, s)))
            {
                world = RefundFromNonValidator(context, world, ta);
            }

            return world;
        }

        private IWorld RefundFromNonValidator(IActionContext context, IWorld world, (Address, int) ta)
        {
            var (target, amount) = ta;
            var stakeStateAddress = StakeState.DeriveAddress(target);

            return world.TransferAsset(
                context,
                Addresses.NonValidatorDelegatee,
                stakeStateAddress,
                Currencies.GuildGold * amount);
        }
    }
}
