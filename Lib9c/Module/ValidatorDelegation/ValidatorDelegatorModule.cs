#nullable enable
using System.Diagnostics.CodeAnalysis;
using Libplanet.Crypto;
using Nekoyume.ValidatorDelegation;

namespace Nekoyume.Module.ValidatorDelegation
{
    public static class ValidatorDelegatorModule
    {
        public static bool TryGetValidatorDelegator(
            this ValidatorRepository repository,
            Address address,
            [NotNullWhen(true)] out ValidatorDelegator? validatorDelegator)
        {
            try
            {
                validatorDelegator = repository.GetValidatorDelegator(address);
                return true;
            }
            catch
            {
                validatorDelegator = null;
                return false;
            }
        }
    }
}
