# WorldInformation

WorldInformation exists for each avatar and contains information about the avatar's adventures.

- [WorldInformation](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Model/WorldInformation.cs)

### State

- Account Address: [Addresses.WorldInformation](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Addresses.cs#L46)
- State Address: Use the address of your avatar as it is.

##### Get State:

```cs
public WorldInformation? GetWorldInformation(IWorld world, Address address)
{
    IAccount account = world.GetAccount(Addresses.WorldInformation);
    if (account is null)
    {
        return null;
    }

    IValue state = account.GetState(address);
    return state switch
    {
        Bencodex.Types.Dictionary d => new WorldInformation(d),
        _ => null,
    };
}
```
