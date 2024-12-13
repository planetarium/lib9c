# @planetarium/9c-headless-provider

A library to provide `TxMetadataProvider` implementation for [NineChronicles.Headless][9c-headless].

[9c-headless]: https://github.com/planetarium/NineChronicles

## Usage Example

```typescript
import { HeadlessClient } from "@planetarium/9c-headless-provider";
import { RawPrivateKey, Address } from "@planetarium/account";
import { signTx } from "@planetarium/tx";
import { makeTx, ClaimStakeReward } from "@planetarium/lib9c";

const headlessClient = new HeadlessClient("https://9c-main-full-state.nine-chronicles.com/graphql");
const account = RawPrivateKey.generate();  // Temporary private key key.

const unsignedTx = await makeTx(account, headlessClient, new ClaimStakeReward({
  avatarAddress: Address.fromHex('<ADDRESS>'),
}));

console.log(unsignedTx);

const signedTx = await signTx(unsignedTx, account);

console.log(signedTx);

const txId = await headlessClient.stageTransaction(signedTx);

console.log(txId);
```
