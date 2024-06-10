import { Buffer } from "buffer";
import type { Currency, FungibleAssetValue } from "@planetarium/tx";
import { Decimal } from "decimal.js";

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

export const MEAD: Currency = {
  ticker: "Mead",
  decimalPlaces: 18,
  minters: null,
  maximumSupply: null,
  totalSupplyTrackable: false,
};

export const CRYSTAL: Currency = {
  ticker: "CRYSTAL",
  decimalPlaces: 18,
  minters: null,
  maximumSupply: null,
  totalSupplyTrackable: false,
};

export const GARAGE: Currency = {
  ticker: "GARAGE",
  decimalPlaces: 18,
  minters: null,
  maximumSupply: null,
  totalSupplyTrackable: false,
};

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
