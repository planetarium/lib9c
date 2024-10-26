import { Buffer } from "buffer";

/**
 * The genesis hash of the Odin network.
 */
export const ODIN_GENESIS_HASH: Uint8Array = Buffer.from(
  "4582250d0da33b06779a8475d283d5dd210c683b9b999d74d03fac4f58fa6bce",
  "hex",
);

/**
 * The genesis hash of the Heimdall network.
 */
export const HEIMDALL_GENESIS_HASH: Uint8Array = Buffer.from(
  "729fa26958648a35b53e8e3905d11ec53b1b4929bf5f499884aed7df616f5913",
  "hex",
);
