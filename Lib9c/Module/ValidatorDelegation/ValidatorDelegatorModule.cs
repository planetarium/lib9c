#nullable enable
using System.Diagnostics.CodeAnalysis;
using Lib9c.ValidatorDelegation;
using Libplanet.Crypto;

namespace Lib9c.Module.ValidatorDelegation
{
    public static class ValidatorDelegatorModule
    {
        public static bool TryGetDelegator(
            this ValidatorRepository repository,
            Address address,
            [NotNullWhen(true)] out ValidatorDelegator? validatorDelegator)
        {
            try
            {
                validatorDelegator = repository.GetDelegator(address);
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
