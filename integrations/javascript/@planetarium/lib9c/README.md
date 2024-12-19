# @planetarium/lib9c

This npm package provides functions to build actions equivalent to [Lib9c].

[Lib9c]: https://github.com/planetarium/lib9c

## Usage Example

```typescript
import { RawPrivateKey, Address } from "@planetarium/account";
import { signTx } from "@planetarium/tx";
import { makeTx, ClaimStakeReward, type TxMetadataProvider } from "@planetarium/lib9c";

const provider: TxMetadataProvider;  // You can use jsr:@planetarium/9c-headless-provider
const account = RawPrivateKey.generate();  // Temporary private key key.

const unsignedTx = await makeTx(account, networkProvider, new ClaimStakeReward({
  avatarAddress: Address.fromHex('<ADDRESS>'),
}));

console.log(unsignedTx);

const signedTx = await signTx(unsignedTx, account);

console.log(signedTx);
```
