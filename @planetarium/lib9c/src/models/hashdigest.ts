import { Buffer } from "buffer";

const algorithms = {
  SHA256: {
    length: 32,
  },
} as const;
type Algorithms = typeof algorithms;
type AlgorithmNames = keyof Algorithms;

export class HashDigest<const T extends AlgorithmNames> {
  private readonly raw: Uint8Array;

  constructor(algorithmName: T, raw: Uint8Array) {
    const length = algorithms[algorithmName].length;

    if (raw.length !== length) {
      throw TypeError(
        `Expected ${length}-length byte array for ${algorithmName} but ${length}-length byte array is given.`,
      );
    }

    this.raw = raw;
  }

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
