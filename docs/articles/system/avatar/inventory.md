# Inventory

Inventories exist for each avatar and contain a variety of items.

- [Inventory](https://github.com/planetarium/lib9c/blob/main/Lib9c/Model/Item/Inventory.cs)

### State

- Account Address: [Addresses.Inventory](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Addresses.cs#L45)
- State Address: Use the address of the avatar as it is.

##### Get State:

```cs
public Inventory? GetInventory(IWorld world, Address address)
{
    IAccount account = world.GetAccount(Addresses.Inventory);
    if (account is null)
    {
        return null;
    }

    IValue state = account.GetState(address);
    return state switch
    {
        Bencodex.Types.List l => new Inventory(l),
        _ => null,
    };
}
```
