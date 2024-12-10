import { type Account, Address } from "@planetarium/account";
import type { UnsignedTx } from "@planetarium/tx";
import type { PolymorphicAction } from "../actions/common.js";
import { TransferAsset } from "../actions/transfer_asset.js";
import { TransferAssets } from "../actions/transfer_assets.js";
import { MEAD, fav } from "../models/currencies.js";

export interface TxMetadataProvider {
  getNextNonce(address: Address): Promise<bigint>;
  getGenesisHash(): Promise<Uint8Array>;
}

export async function makeTx(
  account: Account,
  provider: TxMetadataProvider,
  action: PolymorphicAction,
): Promise<UnsignedTx> {
  const publicKey = await account.getPublicKey();
  const signer = Address.deriveFrom(publicKey);
  const nonce = await provider.getNextNonce(signer);
  const genesisHash = await provider.getGenesisHash();

  const gasLimit =
    action instanceof TransferAsset || action instanceof TransferAssets
      ? 4n
      : 1n;

  return {
    nonce,
    genesisHash: genesisHash,
    signer: signer.toBytes(),
    updatedAddresses: new Set(),
    actions: [action.bencode()],
    publicKey: publicKey.toBytes("uncompressed"),
    timestamp: new Date(),
    gasLimit,
    maxGasPrice: fav(MEAD, 1n),
  };
}
