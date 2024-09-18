# Avatar(AvatarState)

An avatar is a state that corresponds to a character, containing their name, level, and more.

- [AvatarState](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Model/State/AvatarState.cs)

### State

- Account Address: [Addresses.Avatar](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Addresses.cs#L44)
- State Address: The avatar's address is derived from the agent's address.

```cs
public Address GetAvatarAddress(Address agentAddress, int index)
{
    return agentAddress.Derive($"avatar-state-{index}");
    // or
    return Addresses.GetAvatarAddress(agentAddress, index);
}
```

##### Get State:

```cs
public AvatarState? GetAvatarState(IWorld world, Address address)
{
    IAccount account = world.GetAccount(Addresses.Avatar);
    if (account is null)
    {
        return null;
    }

    IValue state = account.GetState(address);
    return state switch
    {
        Bencodex.Types.List l => new AvatarState(l),
        Bencodex.Types.Dictionary d => new AvatarState(d),
        _ => null,
    };
}
```
