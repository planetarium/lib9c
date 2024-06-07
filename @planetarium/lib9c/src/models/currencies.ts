import { Buffer } from "buffer";
import type { Currency, FungibleAssetValue } from "@planetarium/tx";
import { Decimal } from "decimal.js";

/**
 * The main currency of the Nine Chronicles. It should be used in the Odin mainnet network.
 */
export const NCG: Currency = {
  ticker: "NCG",
  decimalPlaces: 2,
  minters: new Set([
    Buffer.from("47d082a115c63e7b58b1532d20e631538eafadde", "hex"),
  ]),
  maximumSupply: null,
  totalSupplyTrackable: false,
};

/**
 * NCG but without minters. It can used as NCG in heimdall mainnet network.
 */
export const MINTERLESS_NCG: Currency = {
  ticker: "NCG",
  decimalPlaces: 2,
  minters: null,
  maximumSupply: null,
  totalSupplyTrackable: false,
};

/**
 * MEAD is the currency used for transaction fee in the Nine Chronicles.
 */
export const MEAD: Currency = {
  ticker: "Mead",
  decimalPlaces: 18,
  minters: null,
  maximumSupply: null,
  totalSupplyTrackable: false,
};

/**
 * CRYSTAL is the currency used for crafting and upgrading items in the Nine Chronicles.
 */
export const CRYSTAL: Currency = {
  ticker: "CRYSTAL",
  decimalPlaces: 18,
  minters: null,
  maximumSupply: null,
  totalSupplyTrackable: false,
};

/**
 * GARAGE is the currency used to make items or fungible asset values able to transfer to others.
 * See [NCIP-16](https://github.com/planetarium/NCIPs/blob/main/NCIP/ncip-16.md)
 */
export const GARAGE: Currency = {
  ticker: "GARAGE",
  decimalPlaces: 18,
  minters: null,
  maximumSupply: null,
  totalSupplyTrackable: false,
};

/**
 * Creates a new `FungibleAssetValue` with the given `currency` and `number-like` values. If the `number-like` value is a `string`, it is parsed and multiplied by 10 times `currency.decimalPlaces` to return the FungibleAssetValue. The same is true if it is a `number`. However, in the case of `number`, beware of the possibility of an incorrect decimal value. The same is true for `bigint`, but due to the nature of the `bigint` type, decimals are not allowed.
 * @param currency The currency of the value.
 * @param numberLike The amount of the given currency.
 * @returns
 * @example To create a FungibleAssetValue for 1 NCG:
 * ```typescript
 * fav(NCG, 1); // With number
 * fav(NCG, "1"); // With string
 * fav(NCG, 1n); // With bigint
 * ```
 * @example To create a FungibleAssetValue for 1.23 NCG:
 * ```typescript
 * fav(NCG, 1.23); // With number
 * fav(NCG, "1.23"); // With string
 * // With bigint, it cannot be created because bigint doesn't represents decimal values.
 * ```
 */
export function fav(
  currency: Currency,
  numberLike: string | number | bigint,
): FungibleAssetValue {
  const rawValue = BigInt(
    new Decimal(
      typeof numberLike === "bigint" ? numberLike.toString() : numberLike,
    )
      .mul(Decimal.pow(10, currency.decimalPlaces))
      .toString(),
  );
  return {
    currency,
    rawValue,
  };
}
