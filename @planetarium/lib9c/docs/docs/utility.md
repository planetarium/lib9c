# Utility

## Currency

### NCG

Nine Chronicles has a currency called *Nine Chronicles Gold*, abbreviated as “NCG”. It is used for staking, called Monster Collections. It can also be used to purchase or enhance items, or to purchase Arena tickets.

```typescript
import { NCG } from “@planetarium/lib9c”;
```

However, Nine Chronicles operates two networks, Odin and Heimdall, under a system called Multiplanetary. Both are valid mainnets, but the WNCG bridge is unique to Odin. On Heimdall, you should use `MINTERLESS_NCG` instead of `NCG`.

```typescript
import { MINTELESS_NCG } from “@planetarium/lib9c”;
```

### CRYSTAL

'CRYSTAL' is the currency used to unlock and craft item recipes and participate in the Arena. It can be obtained through staking rewards.

```typescript
import { CRYSTAL } from “@planetarium/lib9c”;
```

### MEAD

'MEAD' is a currency used as a transaction fee on the Nine Chronicles network.

```typescript
import { MEAD } from “@planetarium/lib9c”;
```

### GARAGE

'GARAGE' is used to move non-transferable goods. See [NCIP-16](https://github.com/planetarium/NCIPs/blob/main/NCIP/ncip-16.md).

```typescript
import { GARAGE } from “@planetarium/lib9c”;
```

## Utility functions

### `fav`

To create a `FungibleAssetValue` from the original `@planetarium/tx`, we need to write the code below. The code below creates a `FungibleAssetValue` that means `10 CRYSTAL`.

```typescript
const fav: FungibleAssetValue = {
  currency: CRYSTAL,
  rawValue: BigInt(new Decimal(10).mul(Decimal.pow(10, CRYSTAL.decimalPlaces)).toString())
}
```

The reason we do this with `decimal.js` is because `FungibleAssetValue` (hereafter `FAV`) doesn't otherwise support decimals when displaying quantities. Not supporting decimals doesn't really mean it doesn't support decimals, it just defines the decimal places by the value in `Currency.decimalPlaces` while keeping the actual value as an integer.

For example, when creating a `2 NCG`, the `FAV.rawValue` is `200n`. Because `NCG.decimalPlaces` is 2, the `00` part is used as the decimal place and the `2` part is used as the natural number part, which is expressed as `2.00 NCG` in common decimal notation.

The `fav` function is a utility function that allows you to abbreviate code like this. The above code would look like this as a `fav` function

```typescript
fav(NCG, 2);
```

However, it is not recommended to use the `number` type to handle prime numbers because there is an error in floating point numbers. In addition to prime numbers, there are also limits on the size of numbers, so you should use `bigint`, `string`, or `decimal.js`. If you do use `decimal.js`, make it a `string` type with `.toString()` and pass it to us.

```typescript
import { fav } from “@planetarium/lib9c”;
```
