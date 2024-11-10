using System;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Module.ValidatorDelegation;
using Nekoyume.ValidatorDelegation;
using Nekoyume.Model.Guild;
using Nekoyume.Module.Guild;

namespace Nekoyume.Action.ValidatorDelegation
{
    [ActionType(TypeIdentifier)]
    public sealed class PromoteValidator : ActionBase
    {
        public const string TypeIdentifier = "promote_validator";

        public PromoteValidator() { }

        public PromoteValidator(PublicKey publicKey, FungibleAssetValue fav)
            : this(publicKey, fav, ValidatorDelegatee.DefaultCommissionPercentage)
        {
        }

        public PromoteValidator(PublicKey publicKey, FungibleAssetValue fav, BigInteger commissionPercentage)
        {
            PublicKey = publicKey;
            FAV = fav;
            CommissionPercentage = commissionPercentage;
        }

        public PublicKey PublicKey { get; private set; }

        public FungibleAssetValue FAV { get; private set; }

        public BigInteger CommissionPercentage { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", List.Empty
                .Add(PublicKey.Format(true))
                .Add(FAV.Serialize())
                .Add(CommissionPercentage));

        public override void LoadPlainValue(IValue plainValue)
        {
            var root = (Dictionary)plainValue;
            if (plainValue is not Dictionary ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not List values)
            {
                throw new InvalidCastException();
            }

            PublicKey = new PublicKey(((Binary)values[0]).ByteArray);
            FAV = new FungibleAssetValue(values[1]);
            CommissionPercentage = (Integer)values[2];
        }

        // TODO: Remove this with ExecutePublic when to deliver features to users.
        public override IWorld Execute(IActionContext context)
        {
            var world = ExecutePublic(context);
            if (context.Signer != ValidatorConfig.PlanetariumValidatorAddress)
            {
                throw new InvalidOperationException(
                    $"This action is not allowed for {context.Signer}.");
            }

            return world;
        }

        public IWorld ExecutePublic(IActionContext context)
        {
            GasTracer.UseGas(1);

            var world = context.PreviousState;

            var repository = new ValidatorRepository(world, context);
            repository.CreateValidatorDelegatee(PublicKey, CommissionPercentage);
            repository.DelegateValidator(context.Signer, FAV);

            var guildRepository = new GuildRepository(repository.World, context);
            guildRepository.CreateValidatorDelegateeForGuildParticipant();

            return guildRepository.World;
        }
    }
}
