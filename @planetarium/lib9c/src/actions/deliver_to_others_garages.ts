import type { Address } from "@planetarium/account";
import {
  BencodexDictionary,
  type Dictionary,
  type Key,
  type Value,
} from "@planetarium/bencodex";
import {
  type FungibleAssetValue,
  encodeFungibleAssetValue,
} from "@planetarium/tx";
import type { HashDigest } from "../models/hashdigest.js";
import { GameAction } from "./common.js";

export class DeliverToOtherGarages extends GameAction {
  protected readonly type_id: string = "deliver_to_others_garages";

  public readonly recipientAgentAddress: Address;
  public readonly fungibleAssetValues: FungibleAssetValue[] | null;
  public readonly fungibleIdAndCounts: [HashDigest<"SHA256">, bigint][] | null;
  public readonly memo: string | null;

  constructor({
    recipientAgentAddress,
    fungibleAssetValues,
    fungibleIdAndCounts,
    memo,
    id,
  }: {
    recipientAgentAddress: Address;
    fungibleAssetValues?: FungibleAssetValue[];
    fungibleIdAndCounts?: [HashDigest<"SHA256">, bigint][];
    memo?: string;
    id?: Uint8Array;
  }) {
    super(id);

    this.recipientAgentAddress = recipientAgentAddress;
    this.fungibleAssetValues = fungibleAssetValues || null;
    this.fungibleIdAndCounts = fungibleIdAndCounts || null;
    this.memo = memo || null;
  }

  protected plain_value_internal(): Dictionary {
    const params: [Key, Value][] = [
      [
        "l",
        [
          this.recipientAgentAddress.toBytes(),
          this.fungibleAssetValues !== null
            ? this.fungibleAssetValues.map((x) => encodeFungibleAssetValue(x))
            : null,
          this.fungibleIdAndCounts !== null
            ? this.fungibleIdAndCounts.map((xs) => [xs[0].toBytes(), xs[1]])
            : null,
          this.memo,
        ],
      ],
    ];

    return new BencodexDictionary(params);
  }
}
