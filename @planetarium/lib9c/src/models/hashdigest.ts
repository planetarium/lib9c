import { Buffer } from "buffer";

const algorithms = {
  SHA256: {
    length: 32,
  },
} as const;
type Algorithms = typeof algorithms;

/**
 * The hash algorithms used by Nine Chronicles. Currently, only "SHA256" is supported.
 */
export type AlgorithmNames = keyof Algorithms;

/**
 * Represents a hash digest. It is the same with [Libplanet HashDigeset](https://docs.libplanet.io/4.6.0/api/Libplanet.Common.HashDigest-1.html).
 */
export class HashDigest<const T extends AlgorithmNames> {
  private readonly raw: Uint8Array;

  /**
   * Create a HashDigest from a raw byte array.
   * @param algorithmName The one of the algorithm names. (e.g., "SHA256")
   * @param raw The raw byte array of the hash digest.
   */
  constructor(algorithmName: T, raw: Uint8Array) {
    const length = algorithms[algorithmName].length;

    if (raw.length !== length) {
      throw TypeError(
        `Expected ${length}-length byte array for ${algorithmName} but ${length}-length byte array is given.`,
      );
    }

    this.raw = raw;
  }

  /**
   * Create a HashDigest from a hexadecimal string.
   * @param algorithmName The one of the algorithm names. (e.g., "SHA256")
   * @param hex The hexadecimal string to convert to HashDigest.
   * @returns
   * @example
   * ```typescript
   * HashDigest.fromHex("SHA256", "4582250d0da33b06779a8475d283d5dd210c683b9b999d74d03fac4f58fa6bce")
   * ```
   */
  static fromHex<T extends AlgorithmNames>(
    algorithmName: T,
    hex: string,
  ): HashDigest<T> {
    return new HashDigest(algorithmName, Buffer.from(hex, "hex"));
  }

  toBytes(): Uint8Array {
    return this.raw;
  }
}
