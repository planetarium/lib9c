import type { Address } from "@planetarium/account";
import { encode } from "@planetarium/bencodex";
import type { TxMetadataProvider } from "@planetarium/lib9c";
import { encodeSignedTx, type signTx } from "@planetarium/tx";
import { GraphQLClient } from "graphql-request";
import { type Sdk, getSdk } from "./generated/headless/graphql-request.js";

export type SignedTx = Awaited<ReturnType<typeof signTx>>;
function serializeSignedTx(signedTx: SignedTx): Uint8Array {
  return encode(encodeSignedTx(signedTx));
}

export class HeadlessClient implements TxMetadataProvider {
  private constructor(private readonly sdk: Sdk) {}

  public static create(url: string): HeadlessClient {
    const client = new GraphQLClient(url);
    return new HeadlessClient(getSdk(client));
  }

  async getNextNonce(address: Address): Promise<bigint> {
    const response = await this.sdk.GetNextNonce({
      address: address.toString(),
    });

    return BigInt(response.nextTxNonce);
  }

  async getGenesisHash(): Promise<Uint8Array> {
    const response = await this.sdk.GetGenesisHash();
    return Buffer.from(response.nodeStatus.genesis.hash, "hex");
  }

  async stageTransaction(signedTx: SignedTx): Promise<string> {
    const serialized = serializeSignedTx(signedTx);
    const response = await this.sdk.StageTransaction({
      tx: Buffer.from(serialized).toString("hex"),
    });

    return response.stageTransaction;
  }
}
