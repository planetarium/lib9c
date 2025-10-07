using Lib9c.Extensions;
using Lib9c.ValidatorDelegation;

namespace Lib9c.Module.ValidatorDelegation
{
    public static class AbstainHistoryModule
    {
        public static AbstainHistory GetAbstainHistory(
            this ValidatorRepository repository)
        {
            try
            {
                return new AbstainHistory(
                    repository.World
                        .GetAccount(Addresses.AbstainHistory)
                        .GetState(AbstainHistory.Address));
            }
            catch
            {
                return new AbstainHistory();
            }
        }

        public static ValidatorRepository SetAbstainHistory(
            this ValidatorRepository repository,
            AbstainHistory abstainHistory)
        {
            repository.UpdateWorld(
                repository.World
                    .MutateAccount(
                        Addresses.AbstainHistory,
                        account => account.SetState(AbstainHistory.Address, abstainHistory.Bencoded)));

            return repository;
        }
    }
}
