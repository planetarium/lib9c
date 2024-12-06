import { Decimal } from "decimal.js";
import { afterAll, beforeEach, describe, expect, it } from "vitest";
import { MEAD, NCG, fav } from "../../src/index.js";

describe("fav", () => {
  beforeEach(() => {
    Decimal.set({
      defaults: true,
    });
  });

  afterAll(() => {
    Decimal.set({
      defaults: true,
    });
  });

  it("should be able to handle FungibleAssetValue with big quantity", () => {
    Decimal.set({
      toExpPos: 900000000000000,
    });

    expect(fav(MEAD, 100_0000n)).toStrictEqual({
      currency: MEAD,
      rawValue: 1000000000000000000000000n,
    });
  });

  it("should throw error with parameter having more decimal places than the maximum supported by currency", () => {
    Decimal.set({
      defaults: true,
    });

    // NCG.decimalPlaces = 2.
    expect(() => fav(NCG, "0.001")).toThrowError(
      /parameter seems to have more decimal places than the maximum supported by currency/,
    );
  });
});
