using System;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Model.Guild;
using Nekoyume.Module.Guild;
using Nekoyume.Module.ValidatorDelegation;
using Nekoyume.TypedAddress;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Action.Guild
{
    [ActionType(TypeIdentifier)]
    public class MakeGuild : ActionBase
    {
        public const string TypeIdentifier = "make_guild";

        private const string ValidatorAddressKey = "va";

        public MakeGuild() { }

        public MakeGuild(Address validatorAddress)
        {
            ValidatorAddress = validatorAddress;
        }

        public Address ValidatorAddress { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", TypeIdentifier)
            .Add("values", Dictionary.Empty
                .Add(ValidatorAddressKey, ValidatorAddress.Bencoded));

        public override void LoadPlainValue(IValue plainValue)
        {
            if (plainValue is not Dictionary root ||
                !root.TryGetValue((Text)"values", out var rawValues) ||
                rawValues is not Dictionary values ||
                !values.TryGetValue((Text)ValidatorAddressKey, out var rawValidatorAddress))
            {
                throw new InvalidCastException();
            }

            ValidatorAddress = new Address(rawValidatorAddress);
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(1);

            var world = context.PreviousState;
            var random = context.GetRandom();
            var repository = new GuildRepository(world, context);
            var guildAddress = new GuildAddress(random.GenerateAddress());
            var validatorAddress = ValidatorAddress;

            repository.MakeGuild(guildAddress, validatorAddress);

            return repository.World;
        }
    }
}
