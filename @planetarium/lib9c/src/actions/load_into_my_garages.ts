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

export class LoadIntoMyGarages extends GameAction {
  protected readonly type_id: string = "load_into_my_garages";

  public readonly avatarAddress: Address | null;
  public readonly fungibleAssetValues: [Address, FungibleAssetValue][] | null;
  public readonly fungibleIdAndCounts: [HashDigest<"SHA256">, bigint][] | null;
  public readonly memo: string | null;

  constructor({
    avatarAddress,
    fungibleAssetValues,
    fungibleIdAndCounts,
    memo,
    id,
  }: {
    avatarAddress?: Address;
    fungibleIdAndCounts?: [HashDigest<"SHA256">, bigint][];
    fungibleAssetValues?: [Address, FungibleAssetValue][];
    memo?: string;
    id?: Uint8Array;
  }) {
    super(id);

    this.avatarAddress = avatarAddress || null;
    this.fungibleAssetValues = fungibleAssetValues || null;
    this.fungibleIdAndCounts = fungibleIdAndCounts || null;
    this.memo = memo || null;
  }

  protected plain_value_internal(): Dictionary {
    const params: [Key, Value][] = [
      [
        "l",
        [
          this.fungibleAssetValues !== null
            ? this.fungibleAssetValues.map((xs) => [
                xs[0].toBytes(),
                encodeFungibleAssetValue(xs[1]),
              ])
            : null,
          this.avatarAddress !== null ? this.avatarAddress?.toBytes() : null,
          this.fungibleIdAndCounts !== null
            ? this.fungibleIdAndCounts?.map((xs) => [xs[0].toBytes(), xs[1]])
            : null,
          this.memo,
        ],
      ],
    ];

    return new BencodexDictionary(params);
  }
}
