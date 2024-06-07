# Actions

## TransferAsset - Transfer

This action is used when you want to transfer a `FungibleAssetValue` that you own to another address.

```typescript
import { TransferAsset, NCG, fav } from “@planetarium/lib9c”;
import { Address } from “@planetarium/account”;

const action = new TransferAsset({
  sender: Address.fromHex('0x491d9842ed8f1b5d291272cf9e7b66a7b7c90cda', true),
  recipient: Address.fromHex('0xfee0bfb15fb3fe521560cbdb308ece4457d13cfa', true),
  amount: fav(NCG, 2),
});
```

If you want to send money to the Ethereum WNCG bridge and convert it to Ethereum WNCG, create an action as follows. This is only valid on the Odin mainnet network.

```typescript
import { TransferAsset, NCG, fav } from “@planetarium/lib9c”;
import { Address } from “@planetarium/account”;

const MY_ADDRESS = Address.fromHex('<YOUR ADDRESS>', true); // [!code warning] Replace with your address.
const ETHEREUM_BRIDGE_ADDRESS = Address.fromHex('0x9093dd96c4bb6b44a9e0a522e2de49641f146223', true);
const AMOUNT = '100.0'; // [!code warning] Replace with the amount you want to bridge.

const action = new TransferAsset({
  sender: MY_ADDRESS,
  recipient: ETHEREUM_BRIDGE_ADDRESS,
  amount: fav(NCG, AMOUNT),
});
```

## TransferAssets - Multiple transfers

This action is used when you want to transfer a number of `FungibleAssetValue`s you own to different addresses. The example below sends 2 NCG to each address.

```typescript
import { TransferAsses, NCG, fav } from “@planetarium/lib9c”;
import { Address } from “@planetarium/account”;

const sender = Address.fromHex('0x2cBaDf26574756119cF705289C33710F27443767');
const agentAddresses = [
  Address.fromHex(“0xfee0bfb15fb3fe521560cbdb308ece4457d13cfa”, true),
  Address.fromHex(“0x491d9842ed8f1b5d291272cf9e7b66a7b7c90cda”, true),
];
const recipients = agentAddresses.map((address) => [address, fav(NCG, 2)]);
const action = new TransferAssets({
  sender,
  recipients,
});
```

## DailyReward - Recharges Action Points

```typescript
import { DailyReward } from “@planetarium/lib9c”;
import { Address } from “@planetarium/account”;

const action = new DailyReward({
  avatarAddress: Address.fromHex('0xDE3873DB166647Cc3538ef64EAA8A0cCFD51B9fE'),
});
```

## Stake - Staking

This action is used when you want to proceed with monster collection (staking). Deposits an amount of NCG equal to `amount`. See [NCIP-17] for implementation definitions.

```typescript
import { Stake } from “@planetarium/lib9c”;

const action = new Stake({
  amount: 100n,
});
```

## ClaimStakeReward - Claiming a Stake Reward

This action is used when you want to claim a Monster Collection (Stake) reward. The `Stake` action will claim the reward corresponding to the NCG you have etched. Requires an `avatarAddress` to receive the reward. See [NCIP-17] for implementation definitions.

```typescript
import { ClaimStakeReward } from “@planetarium/lib9c”;
import { Address } from “@planetarium/account”;

const action = new ClaimStakeReward({
  avatarAddress: Address.fromHex('0xDE3873DB166647Cc3538ef64EAA8A0cCFD51B9fE'),
});
```

[NCIP-17]: https://github.com/planetarium/NCIPs/blob/main/NCIP/ncip-17.md
