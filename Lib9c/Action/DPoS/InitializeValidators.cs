using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action.DPoS.Control;
using Nekoyume.Action.DPoS.Misc;
using Nekoyume.Action.DPoS.Util;
using Nekoyume.Module;
using Serilog;

namespace Nekoyume.Action.DPoS
{
    [ActionType(ActionTypeValue)]
    public sealed class InitializeValidators : ActionBase
    {
        private const string ActionTypeValue = "initialize_validators";

        public InitializeValidators(Dictionary<PublicKey, BigInteger> validators)
        {
            Validators = validators.ToImmutableDictionary();
        }

        public InitializeValidators()
        {
        }

        public ImmutableDictionary<PublicKey, BigInteger> Validators { get; set; }

        public override IValue PlainValue {
            get
            {
                var validators = Dictionary.Empty;
#pragma warning disable LAA1002
                foreach (var (validator, power) in Validators)
                {
                    validators = validators.Add((Binary)validator.Serialize(), power);
                }
#pragma warning restore LAA1002

                return Dictionary.Empty
                    .Add("type_id", new Text(ActionTypeValue))
                    .Add("validators", validators);
            }
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var dict = (Bencodex.Types.Dictionary)plainValue;
            var validatorDict = (Dictionary)dict["validators"];
            Validators = validatorDict.Select(
                pair =>
                {
                    var key = pair.Key.ToPublicKey();
                    var power = (Integer)pair.Value;
                    return new KeyValuePair<PublicKey, BigInteger>(key, power);
                }).ToImmutableDictionary();
        }

        public override IWorld Execute(IActionContext context)
        {
            Log.Debug("InitializeValidators");
            context.UseGas(1);
            var states = context.PreviousState;

            var nativeTokens = ImmutableHashSet.Create(
                Asset.GovernanceToken, Asset.ConsensusToken, Asset.Share);
#pragma warning disable LAA1002
            foreach(var (validator, power) in Validators)
            {
                var amount = new FungibleAssetValue(Asset.GovernanceToken, power, 0);
                states = states.MintAsset(
                    context,
                    validator.Address,
                    amount);
                states = ValidatorCtrl.Create(
                    states,
                    context,
                    validator.Address,
                    validator,
                    amount,
                    nativeTokens);
            }
#pragma warning restore LAA1002

            Log.Debug("InitializeValidators complete");
            return states;
        }
    }
}
