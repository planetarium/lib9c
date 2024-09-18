# Agent(AgentState)

An agent is a state corresponding to a player's account, containing a list of addresses for the avatars they own.

- [AgentState](https://github.com/planetarium/lib9c/blob/main/Lib9c/Model/State/AgentState.cs)

### State

- Account Address: [Addresses.Agent](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Addresses.cs#L43)
- State Address: The address of the private key used to play the game.

##### Get State:

```cs
public AgentState? GetAgentState(IWorld world, Address address)
{
    IAccount account = world.GetAccount(Addresses.Agent);
    if (account is null)
    {
        return null;
    }

    IValue state = account.GetState(address);
    return state switch
    {
        Bencodex.Types.List l => new AgentState(l),
        Bencodex.Types.Dictionary d => new AgentState(d),
        _ => null,
    };
}
```
