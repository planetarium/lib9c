using System;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Types.Assets;
using Nekoyume.Model.Stake;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Libplanet.Crypto;

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

        private const string TargetKey = "t";

        public Address Target { get; private set; }

        [Obsolete("Don't call in code.", error: false)]
        public FixToRefundFromNonValidator()
        {
        }

        public FixToRefundFromNonValidator(Address target)
        {
            Target = target;
        }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Dictionary.Empty
                .Add(TargetKey, Target.Bencoded));

        public override void LoadPlainValue(IValue plainValue)
        {
            if (plainValue is not Dictionary root ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not Dictionary values ||
                !values.TryGetValue((Text)TargetKey, out var rawTarget) ||
                rawTarget is not Binary target)
            {
                throw new InvalidCastException();
            }

            Target = new Address(target);
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

            var stakeStateAddress = StakeState.DeriveAddress(Target);

            if (!world.TryGetStakeState(Target, out var stakeState)
                || stakeState.StateVersion != 3)
            {
                throw new InvalidOperationException(
                    "Target is not valid for refunding from non-validator.");
            }

            var ncgStaked = world.GetBalance(stakeStateAddress, world.GetGoldCurrency());
            var ggStaked = world.GetBalance(stakeStateAddress, Currencies.GuildGold);

            var requiredGG = GetGuildCoinFromNCG(ncgStaked) - ggStaked;

            if (requiredGG.Sign != 1)
            {
                throw new InvalidOperationException(
                    "Target has sufficient amount of guild gold.");
            }

            return world.TransferAsset(context, Addresses.NonValidatorDelegatee, stakeStateAddress, requiredGG);
        }

        private static FungibleAssetValue GetGuildCoinFromNCG(FungibleAssetValue balance)
        {
            return FungibleAssetValue.Parse(Currencies.GuildGold,
                balance.GetQuantityString(true));
        }
    }
}
