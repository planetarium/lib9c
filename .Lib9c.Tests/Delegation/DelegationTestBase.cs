namespace Lib9c.Tests.Delegation;

using Lib9c.Model.Guild;
using Lib9c.Model.State;
using Lib9c.Module;
using Lib9c.Module.Guild;
using Lib9c.Module.ValidatorDelegation;
using Lib9c.Tests.Action;
using Lib9c.ValidatorDelegation;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Libplanet.Types.Assets;

public abstract class DelegationTestBase
{
    protected static readonly Currency NCG = Currency.Uncapped("NCG", 2, null);

    public DelegationTestBase()
    {
        var goldCurrencyState = new GoldCurrencyState(NCG);
        World = World
            .SetLegacyState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
    }

    protected IWorld World { get; } = new World(MockUtil.MockModernWorldState);

    protected static IWorld EnsureToCreateDelegatee(IWorld world, PrivateKey validatorKey)
    {
        var actionContext = new ActionContext { };
        var validatorRepository = new ValidatorRepository(world, actionContext);
        validatorRepository.CreateDelegatee(
            validatorKey.PublicKey, commissionPercentage: 10);

        world = validatorRepository.World;
        var repository = new GuildRepository(world, actionContext);
        repository.CreateDelegatee(validatorKey.Address);
        return repository.World;
    }
}
