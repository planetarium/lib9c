Lib9c - A library for Nine Chronicles
=====================================

[![Planetarium Discord Invite](https://img.shields.io/discord/539405872346955788?color=6278DA&label=Planetarium&logo=discord&logoColor=white)](https://bit.ly/3ZxysHz)
[![Planetarium-Dev Discord Invite](https://img.shields.io/discord/928926944937013338?color=6278DA&label=Planetarium-dev&logo=discord&logoColor=white)](https://bit.ly/4dhTLAa)

> [!TIP]
> If you're new to Nine Chronicles, try to visit our **Developer Portal**!
>
> https://nine-chronicles.dev/

Lib9c is a library that contains key implementations
of [Nine Chronicles](https://nine-chronicles.com), a decentralized RPG developed
on [Libplanet](https://libplanet.io).
Lib9c includes Nine Chronicle's key features like in-game decisions and data models, which can be
used to implement game core capabilities.

## Key Features

### Model (Data storing structure, a.k.a. state)

Lib9c runs based on libplanet, blockchain, hence all the data are saved into chain as a `state`.
You can find all models in [Lib9c/Model](Lib9c/Model).
Each model has its own structure and read/write with blockchain store through de/serialization.

### Action (Core game logic changes state)

State can be created/updated by `action`s.
In [Lib9c/Action](Lib9c/Action), you can find all actions that can be executed in the game Nine
Chronicles.
When someone does the action in game, action data is loaded on Libplanet transaction and propagated
through blockchain network.
Then after the transaction is accepted by validators, this action is executed and related states are
updated with action result.

### Data

All game has their own game data such as item table, exp-level table, etc.
Thus Nine Chronicles is a blockchain game, all the gme data also should be stored into blockchain.
To achieve this, special models to handle data are present in [Lib9c/TableData](Lib9c/TableData)
and [Lib9c/TableCSV](Lib9c/TableCSV).
`TableCSV` is actual CSV formed data and this can be handled in Lib9c using `TableData` state model.

## Dependencies

- .Net >= 6.0

## Contribution

Any contributions are welcome. Please check [here](CONTRIBUTING.md).

## License

Lib9c is under GNU GPL3 license. For details, Please check our [LICENSE](LICENSE).
