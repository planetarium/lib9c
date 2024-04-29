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
import { GameAction } from "./common.js";

export class ClaimItems extends GameAction {
  protected readonly type_id: string = "claim_items";

  public readonly claimData: [Address, FungibleAssetValue[]][];
  public readonly memo: string | null;

  constructor({
    claimData,
    memo,
    id,
  }: {
    claimData: [Address, FungibleAssetValue[]][];
    memo?: string;
    id?: Uint8Array;
  }) {
    super(id);

    this.claimData = claimData;
    this.memo = memo || null;
  }

  protected plain_value_internal(): Dictionary {
    const params: [Key, Value][] = [
      [
        "cd",
        this.claimData.map(([address, fungibleAssetValues]) => [
          address.toBytes(),
          fungibleAssetValues.map(fav => encodeFungibleAssetValue(fav)),
        ]),
      ],
    ];

    if (this.memo !== null) {
      params.push(["m", this.memo]);
    }
    console.log(params)
    return new BencodexDictionary(params);
  }
}
