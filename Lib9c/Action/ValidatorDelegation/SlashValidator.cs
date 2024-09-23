using System;
using System.Numerics;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Action;
using Libplanet.Types.Consensus;
using Libplanet.Types.Evidence;
using Nekoyume.ValidatorDelegation;
using System.Linq;
using Nekoyume.Module.ValidatorDelegation;

namespace Nekoyume.Action.ValidatorDelegation
{
    public sealed class SlashValidator : ActionBase
    {
        public SlashValidator()
        {
        }

        public static BigInteger DuplicateVoteSlashFactor => 10;

        public static BigInteger LivenessSlashFactor => 10;

        public static long AbstainJailTime => 10L;

        public override IValue PlainValue => Null.Value;

        public override void LoadPlainValue(IValue plainValue)
        {
        }

        public override IWorld Execute(IActionContext context)
        {
            GasTracer.UseGas(0L);

            var world = context.PreviousState;
            var repository = new ValidatorRepository(world, context);

            var abstainHistory = repository.GetAbstainHistory();
            var abstainsToSlash = abstainHistory.FindToSlashAndAdd(
                context.LastCommit.Votes.Where(vote => vote.Flag == VoteFlag.Null)
                    .Select(vote => vote.ValidatorPublicKey),
                context.BlockIndex);
            repository.SetAbstainHistory(abstainHistory);

            foreach (var abstain in abstainsToSlash)
            {
                var validatorDelegatee = repository.GetValidatorDelegatee(abstain.Address);
                validatorDelegatee.Slash(LivenessSlashFactor, context.BlockIndex, context.BlockIndex);
                validatorDelegatee.Jail(context.BlockIndex + AbstainJailTime, context.BlockIndex);
            }

            foreach (var evidence in context.Evidence)
            {
                switch (evidence)
                {
                    case DuplicateVoteEvidence e:
                        if (e.Height > context.BlockIndex)
                        {
                            throw new Exception("Evidence height is greater than block index.");
                        }

                        var validatorDelegatee = repository.GetValidatorDelegatee(e.TargetAddress);
                        validatorDelegatee.Slash(DuplicateVoteSlashFactor, e.Height, context.BlockIndex);
                        validatorDelegatee.Tombstone();
                        break;
                    default:
                        break;
                }
            }
            
            return repository.World;
        }
    }
}
