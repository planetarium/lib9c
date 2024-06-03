---
outline: deep
---

# Introduction

The `@planetarium/lib9c` library was created to help create the transactions needed to interact with the Nine Chronicles network. It is written in JavaScript/TypeScript for ease of use on the web. It will be very useful if you are creating a service or automated bot on top of the Nine Chronicles network.

There may be some actions that are missing, but we hope the community will add and contribute as needed. There is documentation available, and if you're stuck, please ask for help in the docs.

## Related Libraries

The Nine Chronicles network is built utilizing a .NET-based blockchain library called Libplanet created by Planetarium. Therefore, we recommend using the `@planetarium/tx` and `@planetarium/account` libraries created by the Libplanet team to create and sign transactions. Please refer to their documentation to learn how to use these libraries.

## Caveats when using JavaScript

This library is written in TypeScript. It relies on type checking to ensure that values are passed in with sufficient validation. JavaScript does not have this type checking and this library does not have any logic to dynamically check, so please be aware that if you are using JavaScript, it may be difficult to notice problems caused by these invalid values.

## Discord

[![Planetarium Dev][planetarium-dev-badge]][planetarium-dev-invite-link]

This library was created by the DX team of the Planetarium organization. It resides on the Planetarium Dev discord server and if you have any questions, please leave them on the server in channels like 'NINE CHRONICLES > #general', 'NINE CHRONICLES > #lib9c', etc.


[planetarium-dev-badge]: https://img.shields.io/discord/928926944937013338?color=6278DA&label=Planetarium-dev&logo=discord&logoColor=white
[planetarium-dev-invite-link]: https://discord.com/invite/RYJDyFRYY7
