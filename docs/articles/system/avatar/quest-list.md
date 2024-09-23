# QuestList

QuestList exists for each avatar and contains the avatar's quest information.

- [QuestList](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Model/Quest/QuestList.cs)

### State

- Account Address: [Addresses.QuestList](https://github.com/planetarium/lib9c/blob/1.17.3/Lib9c/Addresses.cs#L47)
- State Address: Use the address of your avatar as it is.

##### Get State:

```cs
public QuestList? GetQuestList(IWorld world, Address address)
{
    IAccount account = world.GetAccount(Addresses.QuestList);
    if (account is null)
    {
        return null;
    }

    IValue state = account.GetState(address);
    return state switch
    {
        Bencodex.Types.List l => new QuestList(l),
        Bencodex.Types.Dictionary d => new QuestList(d),
        _ => null,
    };
}
```
